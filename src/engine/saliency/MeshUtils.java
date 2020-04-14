package engine.saliency;

import java.awt.Color;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.PriorityQueue;
import java.util.Random;
import java.util.Set;

import org.lwjgl.util.vector.Matrix3f;
import org.lwjgl.util.vector.Vector2f;
import org.lwjgl.util.vector.Vector3f;

import engine.util.Obj;
import engine.util.Obj.Face;

public class MeshUtils {
	public static class FaceList extends ArrayList<Face> {
		private static final long serialVersionUID = 5667034252287440718L;
	}

	public static interface SaliencyMapper {
		public float map(Face face, float u, float v, Vector3f position);
	}

	public static interface ColorMapper {
		public static class ColorCode {
			public final Color color;
			public final int code;

			public ColorCode(Color color, int code) {
				this.color = color;
				this.code = code;
			}
		}

		public ColorCode map(Face face, float u, float v, Vector3f position);
	}

	public static List<FeatureSample> selectColorSamples(Obj model, List<FeatureSample> samples, int numNeighbors, float maxNeighborDistance, float minSampleDistance, float thresholdPercentage) {
		class Neighbor {
			FeatureSample sample;
			float dstSq;
		}

		List<FeatureSample> selectedSamples = new ArrayList<>();

		sampleLoop: for(FeatureSample sample : samples) {
			//Check if nearby samples have already been selected
			for(FeatureSample otherSample : selectedSamples) {
				if(Vector3f.sub(sample.position, otherSample.position, new Vector3f()).lengthSquared() < minSampleDistance * minSampleDistance) {
					continue sampleLoop;
				}
			}

			PriorityQueue<Neighbor> neighbors = new PriorityQueue<>((s1, s2) -> -Float.compare(s1.dstSq, s2.dstSq));

			//Find neighbors
			for(FeatureSample otherSample : samples) {
				float dstSq = Vector3f.sub(sample.position, otherSample.position, new Vector3f()).lengthSquared();
				
				if(dstSq < maxNeighborDistance * maxNeighborDistance) {
					Neighbor neighbor = new Neighbor();
					neighbor.sample = otherSample;
					neighbor.dstSq = dstSq;
	
					neighbors.add(neighbor);
	
					//Keep queue small
					if(neighbors.size() > numNeighbors) {
						neighbors.poll();
					}
				}
			}

			//Check how many neighbors have the same code
			int sameCodes = 0;
			for(Neighbor neighbor : neighbors) {
				if(neighbor.sample.color.code == sample.color.code) {
					sameCodes++;
				}
			}

			if((float)sameCodes / neighbors.size() < thresholdPercentage) {
				selectedSamples.add(sample);
			}
		}

		return selectedSamples;
	}

	public static List<FeatureSample> sampleFeatures(Obj model, SaliencyMapper saliencyMapper, ColorMapper colorMapper, List<FeatureSample> samples, int numSamples, Random rng) {
		float[] cumulativeAreas = new float[model.getFaces().size()];

		int i = 0;
		for(Face face : model.getFaces()) {
			float area = faceCross(model, face).length() * 0.5f;
			if(!Float.isFinite(area)) {
				area = 0.0f;
			}
			if(i == 0) {
				cumulativeAreas[i] = area;
			} else {
				cumulativeAreas[i] = cumulativeAreas[i - 1] + area;
			}
			i++;
		}

		for(int j = 0; j < numSamples; j++) {
			float r = rng.nextFloat() * cumulativeAreas[cumulativeAreas.length - 1];

			int index = -1;

			//Binary search for first element > r
			int start = 0;
			int end = cumulativeAreas.length - 1;
			while(start <= end) {
				int mid = (start + end) / 2;
				if(cumulativeAreas[mid] <= r) {
					start = mid + 1;
				} else {
					index = mid;
					end = mid - 1;
				}
			}

			if(index >= 0) {
				Face face = model.getFaces().get(index);

				float u = rng.nextFloat();
				float v = rng.nextFloat();

				if(u + v >= 1.0f) {
					u = 1.0f - u;
					v = 1.0f - v;
				}

				Vector3f a = model.getVertices().get(face.getVertices()[0] - 1);
				Vector3f b = model.getVertices().get(face.getVertices()[1] - 1);
				Vector3f c = model.getVertices().get(face.getVertices()[2] - 1);

				Vector3f d1 = Vector3f.sub(b, a, new Vector3f());
				Vector3f d2 = Vector3f.sub(c, a, new Vector3f());

				Vector3f position = new Vector3f(
						a.x + d1.x * u + d2.x * v,
						a.y + d1.y * u + d2.y * v,
						a.z + d1.z * u + d2.z * v
						);

				samples.add(new FeatureSample(position, saliencyMapper.map(face, u, v, position), colorMapper.map(face, u, v, position)));
			}
		}

		return samples;
	}

	public static FaceList[] mapFaces(Obj model) {
		FaceList[] faceMap = new FaceList[model.getVertices().size()];
		for(Face face : model.getFaces()) {
			for(int i = 0; i < 3; i++) {
				int vertexIndex = face.getVertices()[i] - 1;

				FaceList faceList = faceMap[vertexIndex];
				if(faceList == null) {
					faceMap[vertexIndex] = faceList = new FaceList();
				}

				faceList.add(face);
			}
		}
		return faceMap;
	}

	public static float meanEdgeLength(Obj model) {
		int edges = 0;
		float sum = 0.0f;
		for(Face face : model.getFaces()) {
			sum += Vector3f.sub(model.getVertices().get(face.getVertices()[0] - 1), model.getVertices().get(face.getVertices()[1] - 1), new Vector3f()).length();
			sum += Vector3f.sub(model.getVertices().get(face.getVertices()[1] - 1), model.getVertices().get(face.getVertices()[2] - 1), new Vector3f()).length();
			sum += Vector3f.sub(model.getVertices().get(face.getVertices()[2] - 1), model.getVertices().get(face.getVertices()[0] - 1), new Vector3f()).length();
			edges += 3;
		}
		return sum / edges;
	}

	public static void scaleSurface(Obj model, float surface) {
		float surfaceArea = 0.0f;
		for(Face face : model.getFaces()) {
			surfaceArea += MeshUtils.faceCross(model, face).length() * 0.5f;
		}
		float scale = (float)Math.pow(surface / surfaceArea, 0.5f);
		for(Vector3f vertex : model.getVertices()) {
			vertex.x *= scale;
			vertex.y *= scale;
			vertex.z *= scale;
		}
	}

	public static void applyVertexDiffusion(Obj model, FaceList[] faceMap, float lambda) {
		List<Vector3f> vertices = model.getVertices();

		List<Vector3f> newVertices = new ArrayList<>(vertices.size());

		for(int vertexIndex = 0; vertexIndex < vertices.size(); vertexIndex++) {
			Vector3f vertex = vertices.get(vertexIndex);

			float dx = 0;
			float dy = 0;
			float dz = 0;

			int neighbors = 0;

			FaceList sharedFaces = faceMap[vertexIndex];

			Set<Integer> sharedVertices = new HashSet<>();
			if(sharedFaces != null) {
				for(Face face : sharedFaces) {
					for(int i = 0; i < 3; i++) {
						int otherVertexIndex = face.getVertices()[i] - 1;

						if(otherVertexIndex != vertexIndex) {
							sharedVertices.add(otherVertexIndex);
						}
					}
				}
			}

			for(int sharedVertexIndex : sharedVertices) {
				Vector3f otherVertex = vertices.get(sharedVertexIndex);

				dx += otherVertex.x - vertex.x;
				dy += otherVertex.y - vertex.y;
				dz += otherVertex.z - vertex.z;

				neighbors++;
			}

			if(neighbors > 0) {
				newVertices.add(new Vector3f(
						vertex.x + lambda * dx / neighbors,
						vertex.y + lambda * dy / neighbors,
						vertex.z + lambda * dz / neighbors
						));
			} else {
				newVertices.add(vertex);
			}
		}

		for(int index = 0; index < vertices.size(); index++) {
			vertices.set(index, newVertices.get(index));
		}
	}

	public static void applyScalarDiffusion(Obj model, FaceList[] faceMap, float lambda, float[] vertexScalars) {
		int numVertices = model.getVertices().size();

		float[] newScalars = new float[numVertices];

		for(int vertexIndex = 0; vertexIndex < numVertices; vertexIndex++) {
			float scalar = vertexScalars[vertexIndex];

			float d = 0;

			int neighbors = 0;

			FaceList sharedFaces = faceMap[vertexIndex];

			Set<Integer> sharedVertices = new HashSet<>();
			if(sharedFaces != null) {
				for(Face face : sharedFaces) {
					for(int i = 0; i < 3; i++) {
						int otherVertexIndex = face.getVertices()[i] - 1;

						if(otherVertexIndex != vertexIndex) {
							sharedVertices.add(otherVertexIndex);
						}
					}
				}
			}

			for(int sharedVertexIndex : sharedVertices) {
				d += vertexScalars[sharedVertexIndex] - scalar;
				neighbors++;
			}

			if(neighbors > 0) {
				newScalars[vertexIndex] = scalar + lambda * d / neighbors;
			} else {
				newScalars[vertexIndex] = scalar;
			}
		}

		System.arraycopy(newScalars, 0, vertexScalars, 0, numVertices);
	}

	public static float totalAbsoluteGaussianCurvature(Obj model, FaceList[] faceMap) {
		float totalCurvature = 0.0f;

		for(int i = 0; i < model.getVertices().size(); i++) {
			Vector2f principalCurvatures = MeshUtils.principalCurvatures(model, faceMap, i);

			FaceList sharedFaces = faceMap[i];

			if(sharedFaces != null) {
				float area = 0.0f;
				for(Obj.Face face : sharedFaces) {
					area += MeshUtils.faceCross(model, face).length() * 0.5f;
				}

				float gaussianCurvature = principalCurvatures.x * principalCurvatures.y;
				if(Float.isFinite(gaussianCurvature) && Float.isFinite(area)) {
					totalCurvature += area / 3.0f * Math.abs(gaussianCurvature);
				}
			}
		}

		return totalCurvature;
	}

	public static Vector2f principalCurvatures(Obj model, FaceList[] faceMap, int vertexIndex) {
		Vector3f vertex = model.getVertices().get(vertexIndex);

		FaceList sharedFaces = faceMap[vertexIndex];

		if(sharedFaces == null) {
			return new Vector2f(0, 0);
		}

		Vector3f vertexNormal = weightedNormal(model, sharedFaces);

		Set<Integer> sharedVertices = new HashSet<>();
		for(Face face : sharedFaces) {
			for(int i = 0; i < 3; i++) {
				int otherVertexIndex = face.getVertices()[i] - 1;

				if(otherVertexIndex != vertexIndex) {
					sharedVertices.add(otherVertexIndex);
				}
			}
		}

		//Count faces that are indicent to both vertex and sharedVertex
		int[] weights = new int[sharedVertices.size()];
		int weightSum = 0;

		int i = 0;
		for(int sharedVertexIndex : sharedVertices) {

			for(Face face : sharedFaces) {
				for(int j = 0; j < 3; j++) {
					if(face.getVertices()[j] - 1 == sharedVertexIndex) {
						weights[i]++;
						weightSum++;
						break;
					}
				}
			}

			i++;
		}

		Matrix3f Mvi = new Matrix3f();

		i = 0;
		for(int sharedVertexIndex : sharedVertices) {
			Vector3f sharedVertex = model.getVertices().get(sharedVertexIndex);

			//vj - vi
			Vector3f diff = Vector3f.sub(sharedVertex, vertex, new Vector3f());

			Vector3f Tij = projectOnPlane(diff, vertexNormal).normalise(new Vector3f());

			//kappa i,j
			float kappa = 2.0f * Vector3f.dot(vertexNormal, diff) / diff.lengthSquared();

			float multiplier = weights[i] / (float)weightSum * kappa;

			Mvi.m00 += multiplier * Tij.x * Tij.x;
			Mvi.m01 += multiplier * Tij.x * Tij.y;
			Mvi.m02 += multiplier * Tij.x * Tij.z;
			Mvi.m10 += multiplier * Tij.y * Tij.x;
			Mvi.m11 += multiplier * Tij.y * Tij.y;
			Mvi.m12 += multiplier * Tij.y * Tij.z;
			Mvi.m20 += multiplier * Tij.z * Tij.x;
			Mvi.m21 += multiplier * Tij.z * Tij.y;
			Mvi.m22 += multiplier * Tij.z * Tij.z;

			i++;
		}

		//TODO Doesn't seem to be needed (yet?).
		/*Vector3f E1 = new Vector3f(1, 0, 0);

		Vector3f Wvi;
		if(Vector3f.sub(E1, vertexNormal, new Vector3f()).lengthSquared() > Vector3f.add(E1, vertexNormal, new Vector3f()).lengthSquared()) {
			Wvi = Vector3f.sub(E1, vertexNormal, new Vector3f()).normalise(new Vector3f());
		} else {
			Wvi = Vector3f.add(E1, vertexNormal, new Vector3f()).normalise(new Vector3f());
		}

		Matrix3f Qvi = new Matrix3f();

		Qvi.m00 = 1 - 2 * Wvi.x * Wvi.x;
		Qvi.m01 = 1 - 2 * Wvi.x * Wvi.y;
		Qvi.m02 = 1 - 2 * Wvi.x * Wvi.z;
		Qvi.m10 = 1 - 2 * Wvi.y * Wvi.x;
		Qvi.m11 = 1 - 2 * Wvi.y * Wvi.y;
		Qvi.m12 = 1 - 2 * Wvi.y * Wvi.z;
		Qvi.m20 = 1 - 2 * Wvi.z * Wvi.x;
		Qvi.m21 = 1 - 2 * Wvi.z * Wvi.y;
		Qvi.m22 = 1 - 2 * Wvi.z * Wvi.z;

		Matrix3f M = Matrix3f.mul(Qvi.transpose(new Matrix3f()), Mvi, new Matrix3f());
		M = Matrix3f.mul(M, Qvi, new Matrix3f());*/

		float kappa1 = 3 * Mvi.m11 - Mvi.m22;
		float kappa2 = 3 * Mvi.m22 - Mvi.m11;

		return new Vector2f(kappa1, kappa2);
	}

	public static Vector3f projectOnPlane(Vector3f direction, Vector3f planeNormal) {
		float dot = Vector3f.dot(direction, planeNormal);
		return new Vector3f(
				direction.x - planeNormal.x * dot,
				direction.y - planeNormal.y * dot,
				direction.z - planeNormal.z * dot
				);
	}

	public static Vector3f weightedNormal(Obj model, List<Face> sharedFaces) {
		Vector3f sum = new Vector3f();
		for(Face face : sharedFaces) {
			Vector3f.add(sum, faceCross(model, face), sum);
		}
		return sum.normalise(new Vector3f());
	}

	public static Vector3f faceCross(Obj model, Face face) {
		Vector3f a = model.getVertices().get(face.getVertices()[0] - 1);
		Vector3f b = model.getVertices().get(face.getVertices()[1] - 1);
		Vector3f c = model.getVertices().get(face.getVertices()[2] - 1);
		return Vector3f.cross(Vector3f.sub(b, a, new Vector3f()), Vector3f.sub(c, a, new Vector3f()), new Vector3f());
	}
}
