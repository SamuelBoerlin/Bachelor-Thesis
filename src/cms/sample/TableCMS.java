package cms.sample;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

import cms.sample.HermiteCell.Face;

/**
 * A lookup table based sample implementation intended to be accelerated by the use of GPU algorithms.
 */
public class TableCMS {
	private final QEFSolver qefSolver;

	public TableCMS(QEFSolver qefSolver) {
		this.qefSolver = qefSolver;
	}

	public boolean isSolid(int material) {
		return material != 0;
	}

	private static class CellData {
		public final int baseCase, rotation, ambiguityCount;

		public CellData(int caseIndex) {
			this.baseCase = Tables.RAW_CASE_TO_CASE_ROTATION_AND_AMBIGUITY_COUNT[caseIndex * 3];
			this.rotation = Tables.RAW_CASE_TO_CASE_ROTATION_AND_AMBIGUITY_COUNT[caseIndex * 3 + 1];
			this.ambiguityCount = Tables.RAW_CASE_TO_CASE_ROTATION_AND_AMBIGUITY_COUNT[caseIndex * 3 + 2];
		}
	}

	private float min2DAngle = 35.0f;
	private float min3DAngle = 35.0f;

	/**
	 * Polygonizes a list of Hermite data cells. The output are Cubical Marching Squares components that can either be directly triangulated or processed further.
	 * All loops in this method are intended to be run in parallel on a GPU.
	 * @param cells
	 * @return
	 */
	public List<Component> polygonize(List<HermiteCell> cells) {
		CellData[] cellData = new CellData[cells.size()];

		for(int i = 0; i < cells.size(); i++) {
			int[] materials = this.getMaterials(cells.get(i));

			int caseIndex = 0;
			for(int j = 0; j < 8; j++) {
				caseIndex |= (materials[j] != 0 ? 1 : 0) << j;
			}

			cellData[i] = new CellData(caseIndex);
		}

		int[] ambiguousCaseCountScan = GPU.inclusiveScan(i -> cellData[i].ambiguityCount > 0 ? 1 : 0, cellData.length);
		int numAmbiguousCases = ambiguousCaseCountScan[ambiguousCaseCountScan.length -1];

		int[] ambiguityCountScan = GPU.inclusiveScan(i -> cellData[i].ambiguityCount, cellData.length);
		int numAbiguities = ambiguityCountScan[ambiguityCountScan.length - 1];

		int[] ambiguousCaseCountOccupancy = GPU.occupancy(i -> cellData[i].ambiguityCount > 0 ? 1 : 0, ambiguousCaseCountScan);

		int[] ambiguityCountOccupancy = GPU.occupancy(i -> cellData[i].ambiguityCount, ambiguityCountScan);

		int[] ambiguityResBits = new int[numAbiguities];

		for(int i = 0; i < numAbiguities; i++) {
			int cellIndex = ambiguityCountOccupancy[i];
			int ambiguityNr = ambiguityCountScan[cellIndex] - (i + 1);

			int face = Tables.ROTATION_TO_RAW_FACES[cellData[cellIndex].rotation * 6 + Tables.CASE_AND_AMBIGUITY_NR_TO_FACE[cellData[cellIndex].baseCase * 6 + ambiguityNr]];

			Vec3 faceStart = Tables.FACE_START[face];
			Vec3 faceNormal = Tables.FACE_NORMAL[face];

			int tableIndex = cellData[cellIndex].baseCase * 6 * 4 + ambiguityNr * 4;
			int[] edges = { 
					Tables.CASE_AND_AMBIGUITY_NR_TO_EDGES[tableIndex + 0],
					Tables.CASE_AND_AMBIGUITY_NR_TO_EDGES[tableIndex + 1],
					Tables.CASE_AND_AMBIGUITY_NR_TO_EDGES[tableIndex + 2],
					Tables.CASE_AND_AMBIGUITY_NR_TO_EDGES[tableIndex + 3]
			};

			Vec3[] positions = new Vec3[4];
			Vec3[] normals = new Vec3[4];

			for(int k = 0; k < 4; k++) {
				int edge = edges[k];

				int signedRawEdge = Tables.ROTATION_TO_RAW_SIGNED_EDGES[cellData[cellIndex].rotation * 12 + edge];

				//boolean flip = (signedRawEdge & 0b10000) != 0;
				int rawEdge = signedRawEdge & 0b01111;

				positions[k] = Tables.EDGE_START[rawEdge].add(Tables.EDGE_DIR[rawEdge].mul(this.getIntersection(cells.get(cellIndex), rawEdge)));
				normals[k] = this.getNormal(cells.get(cellIndex), rawEdge);
			}

			Vec3 i01;
			if(Math.toDegrees(Math.acos(VecUtils.projectedNormalizedDot(normals[0], normals[1], faceNormal))) > min2DAngle) {
				i01 = VecUtils.intersectPlanes(positions[0], normals[0], positions[1], normals[1], faceStart, faceNormal);
			} else {
				i01 = positions[0].add(positions[1]).mul(0.5D);
			}

			Vec3 i23;
			if(Math.toDegrees(Math.acos(VecUtils.projectedNormalizedDot(normals[2], normals[3], faceNormal))) > min2DAngle) {
				i23 = VecUtils.intersectPlanes(positions[2], normals[2], positions[3], normals[3], faceStart, faceNormal);
			} else {
				i23 = positions[2].add(positions[3]).mul(0.5D);
			}

			Vec3 axis0 = i01.sub(positions[0]).cross(faceNormal);
			Vec3 axis1 = positions[1].sub(i01).cross(faceNormal);
			Vec3 axis2 = i23.sub(positions[2]).cross(faceNormal);
			Vec3 axis3 = positions[3].sub(i23).cross(faceNormal);

			double min0 = Math.min(axis0.dot(positions[2].sub(i01)), Math.min(axis0.dot(positions[3].sub(i01)), axis0.dot(i23.sub(i01))));
			double min1 = Math.min(axis1.dot(positions[2].sub(i01)), Math.min(axis1.dot(positions[3].sub(i01)), axis1.dot(i23.sub(i01))));
			double min2 = Math.min(axis2.dot(positions[0].sub(i23)), Math.min(axis2.dot(positions[1].sub(i23)), axis2.dot(i01.sub(i23))));
			double min3 = Math.min(axis3.dot(positions[0].sub(i23)), Math.min(axis3.dot(positions[1].sub(i23)), axis3.dot(i01.sub(i23))));

			double max = Math.max(min0, Math.max(min1, Math.max(min2, min3)));

			boolean case0 = max > 0;

			ambiguityResBits[i] = case0 ? 0 : 1;
		}

		int[] ambiguityRes = new int[cellData.length];

		for(int i = 0; i < numAmbiguousCases; i++) {
			int cellIndex = ambiguousCaseCountOccupancy[i];
			int ambiguityCount = cellData[cellIndex].ambiguityCount;

			int ambiguityResBitsIndex = ambiguityCountScan[cellIndex] - ambiguityCount; //since the scan is inclusive we need to remove its own count to get the start index

			int res = 0;
			for(int j = 0; j < ambiguityCount; j++) {
				res |= ambiguityResBits[ambiguityResBitsIndex + j] << (ambiguityCount - 1 - j);
			}

			ambiguityRes[cellIndex] = res;
		}

		int[] componentCount = new int[cellData.length];

		for(int i = 0; i < cellData.length; i++) {
			componentCount[i] = Tables.CASE_AND_AMBIGUITY_RES_TO_SIZE_AND_COMPONENTS[cellData[i].baseCase * 64 * 5 + ambiguityRes[i] * 5];
		}

		int[] componentCountScan = GPU.inclusiveScan(i -> componentCount[i], componentCount.length);
		int numComponents = componentCountScan[componentCountScan.length - 1];

		if(numComponents == 0) {
			return Collections.emptyList();
		}

		int[] componentOccupancy = GPU.occupancy(i -> componentCount[i], componentCountScan);

		int[] componentVertCount = new int[numComponents];

		for(int i = 0; i < numComponents; i++) {
			int cellIndex = componentOccupancy[i];
			int componentIndex = componentCountScan[cellIndex] - (i + 1);

			int componentNr = Tables.CASE_AND_AMBIGUITY_RES_TO_SIZE_AND_COMPONENTS[cellData[cellIndex].baseCase * 64 * 5 + ambiguityRes[cellIndex] * 5 + 1 + componentIndex];

			int componentSize = Tables.COMPONENT_TO_SIZE_AND_EDGES[componentNr * 13];

			int[] rawEdges = new int[componentSize];
			for(int k = 0; k < componentSize; k++) {
				int edge = Tables.COMPONENT_TO_SIZE_AND_EDGES[componentNr * 13 + 1 + k];
				int signedRawEdge = Tables.ROTATION_TO_RAW_SIGNED_EDGES[cellData[cellIndex].rotation * 12 + edge];

				//boolean flip = (signedRawEdge & 0b10000) != 0;
				int rawEdge = signedRawEdge & 0b01111;

				rawEdges[k] = rawEdge;
			}

			int numVerts = componentSize;

			for(int k = 0; k < componentSize; k++) {
				int e1 = rawEdges[k];
				int e2 = rawEdges[(k + 1) % componentSize];
				int face = Tables.EDGE_PAIR_TO_FACE[e1 * 12 + e2];

				Vec3 faceNormal = Tables.FACE_NORMAL[face];

				Vec3 n1 = this.getNormal(cells.get(cellIndex), e1);

				Vec3 n2 = this.getNormal(cells.get(cellIndex), e2);

				double dot = VecUtils.projectedNormalizedDot(n1, n2, faceNormal);

				double angle = Math.toDegrees(Math.acos(dot));

				if(angle > min2DAngle) {
					numVerts++;
				}
			}

			componentVertCount[i] = numVerts;
		}

		int[] componentVertCountScan = GPU.inclusiveScan(i -> componentVertCount[i], componentVertCount.length);
		int numComponentVerts = componentVertCountScan[componentVertCountScan.length - 1];

		Vec3[] vertices = new Vec3[numComponentVerts * 3];

		//TODO These would be either stored directly in GPU memory as triangles or added to an array
		List<Component> components = new ArrayList<>();

		//parallel
		for(int i = 0; i < numComponents; i++) {
			int cellIndex = componentOccupancy[i];
			int componentIndex = componentCountScan[cellIndex] - (i + 1);

			int componentNr = Tables.CASE_AND_AMBIGUITY_RES_TO_SIZE_AND_COMPONENTS[cellData[cellIndex].baseCase * 64 * 5 + ambiguityRes[cellIndex] * 5 + 1 + componentIndex];

			int componentSize = Tables.COMPONENT_TO_SIZE_AND_EDGES[componentNr * 13];

			int[] rawEdges = new int[componentSize];
			for(int k = 0; k < componentSize; k++) {
				int edge = Tables.COMPONENT_TO_SIZE_AND_EDGES[componentNr * 13 + 1 + k];
				int signedRawEdge = Tables.ROTATION_TO_RAW_SIGNED_EDGES[cellData[cellIndex].rotation * 12 + edge];

				//boolean flip = (signedRawEdge & 0b10000) != 0;
				int rawEdge = signedRawEdge & 0b01111;

				rawEdges[k] = rawEdge;
			}

			int numVerts = componentVertCount[i];

			Vec3[] positions = new Vec3[numVerts];
			Vec3[] normals = new Vec3[numVerts];
			for(int j = 0, k = 0; k < componentSize; k++) {
				int e1 = rawEdges[k];

				Vec3 cellPos = cells.get(cellIndex).getCellPosition(cells.get(cellIndex).getCells(Face.Z_NEG)[0]);
				Vec3 p1 = Tables.EDGE_START[e1].add(Tables.EDGE_DIR[e1].mul(this.getIntersection(cells.get(cellIndex), e1)));

				positions[j] = cellPos.add(p1);
				normals[j] = this.getNormal(cells.get(cellIndex), e1);
				j++;

				int e2 = rawEdges[(k + 1) % componentSize];
				int face = Tables.EDGE_PAIR_TO_FACE[e1 * 12 + e2];

				Vec3 faceNormal = Tables.FACE_NORMAL[face];

				Vec3 n1 = this.getNormal(cells.get(cellIndex), e1);

				Vec3 n2 = this.getNormal(cells.get(cellIndex), e2);

				double dot = VecUtils.projectedNormalizedDot(n1, n2, faceNormal);

				double angle = Math.toDegrees(Math.acos(dot));

				if(angle > min2DAngle) {
					Vec3 p2 = Tables.EDGE_START[e2].add(Tables.EDGE_DIR[e2].mul(this.getIntersection(cells.get(cellIndex), e2)));

					positions[j] = cellPos.add(VecUtils.intersectPlanes(p1, n1, p2, n2, Tables.FACE_START[face], faceNormal));
					normals[j] = null;
					j++;
				}
			}

			Vec3 componentQef = this.solveQef(positions, normals);

			int vertIndex = componentVertCountScan[i] - numVerts; //since the scan is inclusive we need to remove its own count to get the start index

			//Create triangle fans
			for(int k = 0; k < numVerts; k++) {
				vertices[vertIndex + k * 3 + 0] = positions[k];
				vertices[vertIndex + k * 3 + 1] = componentQef;
				vertices[vertIndex + k * 3 + 2] = positions[(k + 1) % numVerts];
			}

			//TODO Remove this
			{
				Vec3[] componentVerts = new Vec3[numVerts + 1];
				Vec3[] componentNormals = new Vec3[numVerts + 1];
				int[] componentIndices = new int[numVerts + 1];
				int[] componentMats = new int[numVerts + 1];

				for(int k = 0; k < numVerts; k++) {
					componentVerts[k + 1] = positions[k];
					componentNormals[k + 1] = normals[k];
					componentIndices[k + 1] = Component.packIndex(k * 3 + 0, false, false);
					componentMats[k + 1] = 1;
				}

				componentVerts[0] = componentQef;

				components.add(new Component(componentIndices, componentVerts, componentNormals, componentMats, true, false));
			}
		}

		return components;
	}

	private Vec3 solveQef(Vec3[] positions, Vec3[] normals) {
		int samples = 0;
		for(int i = 0; i < normals.length; i++) {
			if(normals[i] != null) {
				samples++;
			}
		}

		Vec3[] relevantPositions = new Vec3[samples];
		Vec3[] relevantNormals = new Vec3[samples];
		for(int j = 0, i = 0; i < normals.length; i++) {
			if(normals[i] != null) {
				relevantPositions[j] = positions[i];
				relevantNormals[j] = normals[i];
				j++;
			}
		}

		double maxAngle = 0.0f;
		for(int i = 0; i < relevantNormals.length; i++) {
			for(int j = i + 1; j < relevantNormals.length; j++) {
				maxAngle = Math.max(maxAngle, Math.toDegrees(Math.acos(Math.min(1, relevantNormals[i].dot(relevantNormals[j])))));
			}
		}

		Vec3 mean = new Vec3(0, 0, 0);
		for(int k = 0; k < samples; k++) {
			mean = mean.add(relevantPositions[k]);
		}
		mean = mean.mul(1.0D / samples);

		if(maxAngle < min3DAngle) {
			return mean;
		}

		float[][] lhs = new float[samples][3];
		float[][] rhs = new float[samples][1];

		for(int i = 0; i < samples; i++) {
			lhs[i] = new float[] { (float)relevantNormals[i].x, (float)relevantNormals[i].y, (float)relevantNormals[i].z };
		}

		for(int i = 0; i < samples; i++) {
			Vec3 vertex = relevantPositions[i];
			float[] normal = lhs[i];
			rhs[i] = new float[] { (float) (normal[0] * (vertex.x - mean.x) + normal[1] * (vertex.y - mean.y) + normal[2] * (vertex.z - mean.z)) };
		}

		return this.qefSolver.solve(null, lhs, rhs, true /*TODO check if corner feature*/, mean);
	}

	private int[] getCells(HermiteCell cell) {
		return new int[] {
				cell.getCells(Face.Z_NEG)[0],
				cell.getCells(Face.X_POS)[0],
				cell.getCells(Face.Z_POS)[0],
				cell.getCells(Face.X_NEG)[0],
				cell.getCells(Face.Y_NEG)[0],
				cell.getCells(Face.Y_POS)[0]	
		};
	}

	private int[] getMaterials(HermiteCell cell) {
		int[] cells = this.getCells(cell);
		return new int[] {
				cell.getMaterials(cells[0])[0], cell.getMaterials(cells[1])[0], cell.getMaterials(cells[2])[0], cell.getMaterials(cells[3])[0], 
				cell.getMaterials(cells[0])[3], cell.getMaterials(cells[1])[3], cell.getMaterials(cells[2])[3], cell.getMaterials(cells[3])[3]
		};
	}

	private float getIntersection(HermiteCell cell, int edge) {
		int[] cells = this.getCells(cell);

		int[] edges;

		switch(edge) {
		case 0:
			edges = cell.getEdges(cells[0]);
			return cell.getIntersection(cells[0], edges[0]);
		case 9:
			edges = cell.getEdges(cells[0]);
			return cell.getIntersection(cells[0], edges[1]);
		case 4:
			edges = cell.getEdges(cells[0]);
			return 1 - cell.getIntersection(cells[0], edges[2]);
		case 8:
			edges = cell.getEdges(cells[0]);
			return 1 - cell.getIntersection(cells[0], edges[3]);
		case 2:
			edges = cell.getEdges(cells[2]);
			return 1 - cell.getIntersection(cells[2], edges[0]);
		case 11:
			edges = cell.getEdges(cells[2]);
			return cell.getIntersection(cells[2], edges[1]);
		case 6:
			edges = cell.getEdges(cells[2]);
			return cell.getIntersection(cells[2], edges[2]);
		case 10:
			edges = cell.getEdges(cells[2]);
			return 1 - cell.getIntersection(cells[2], edges[3]);
		case 1:
			edges = cell.getEdges(cells[1]);
			return cell.getIntersection(cells[1], edges[0]);
		case 5:
			edges = cell.getEdges(cells[1]);
			return 1 - cell.getIntersection(cells[1], edges[2]);
		case 3:
			edges = cell.getEdges(cells[3]);
			return 1 - cell.getIntersection(cells[3], edges[0]);
		case 7:
			edges = cell.getEdges(cells[3]);
			return cell.getIntersection(cells[3], edges[2]);
		default:
			throw new RuntimeException("Edge out of bounds");
		}
	}

	private Vec3 getNormal(HermiteCell cell, int edge) {
		int[] cells = this.getCells(cell);

		int[] edges;

		switch(edge) {
		case 0:
			edges = cell.getEdges(cells[0]);
			return cell.getNormal(cells[0], edges[0]);
		case 9:
			edges = cell.getEdges(cells[0]);
			return cell.getNormal(cells[0], edges[1]);
		case 4:
			edges = cell.getEdges(cells[0]);
			return cell.getNormal(cells[0], edges[2]);
		case 8:
			edges = cell.getEdges(cells[0]);
			return cell.getNormal(cells[0], edges[3]);
		case 2:
			edges = cell.getEdges(cells[2]);
			return cell.getNormal(cells[2], edges[0]);
		case 11:
			edges = cell.getEdges(cells[2]);
			return cell.getNormal(cells[2], edges[1]);
		case 6:
			edges = cell.getEdges(cells[2]);
			return cell.getNormal(cells[2], edges[2]);
		case 10:
			edges = cell.getEdges(cells[2]);
			return cell.getNormal(cells[2], edges[3]);
		case 1:
			edges = cell.getEdges(cells[1]);
			return cell.getNormal(cells[1], edges[0]);
		case 5:
			edges = cell.getEdges(cells[1]);
			return cell.getNormal(cells[1], edges[2]);
		case 3:
			edges = cell.getEdges(cells[3]);
			return cell.getNormal(cells[3], edges[0]);
		case 7:
			edges = cell.getEdges(cells[3]);
			return cell.getNormal(cells[3], edges[2]);
		default:
			throw new RuntimeException("Edge out of bounds");
		}
	}
}
