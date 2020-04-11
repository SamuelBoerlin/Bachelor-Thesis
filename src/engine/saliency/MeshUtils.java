package engine.saliency;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

import org.lwjgl.util.vector.Matrix3f;
import org.lwjgl.util.vector.Vector2f;
import org.lwjgl.util.vector.Vector3f;

import engine.util.Obj;
import engine.util.Obj.Face;

public class MeshUtils {
	public static void applyDiffusion(Obj model, float lambda) {
		List<Vector3f> vertices = model.getVertices();

		List<Vector3f> newVertices = new ArrayList<>(vertices.size());

		for(int vertexIndex = 0; vertexIndex < vertices.size(); vertexIndex++) {
			Vector3f vertex = vertices.get(vertexIndex);

			float dx = 0;
			float dy = 0;
			float dz = 0;

			int neighbors = 0;

			List<Face> sharedFaces = findFacesWithVertex(model, vertexIndex);

			Set<Integer> sharedVertices = new HashSet<>();
			for(Face face : sharedFaces) {
				for(int i = 0; i < 3; i++) {
					int otherVertexIndex = face.getVertices()[i] - 1;

					if(otherVertexIndex != vertexIndex) {
						sharedVertices.add(otherVertexIndex);
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

	public static Vector2f principalCurvatures(Obj model, int vertexIndex) {
		Vector3f vertex = model.getVertices().get(vertexIndex);

		List<Face> sharedFaces = findFacesWithVertex(model, vertexIndex);

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

			Vector3f Tij = projectOnPlane(Vector3f.sub(sharedVertex, vertex, new Vector3f()), vertexNormal).normalise(new Vector3f());

			//vj - vi
			Vector3f diff = Vector3f.sub(sharedVertex, vertex, new Vector3f());

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

	public static List<Face> findFacesWithVertex(Obj model, int vertexIndex) {
		List<Face> faces = new ArrayList<>();

		for(Face face : model.getFaces()) {
			for(int i = 0; i < 3; i++) {
				if(face.getVertices()[i] - 1 == vertexIndex) {
					faces.add(face);
					break;
				}
			}
		}

		return faces;
	}
}
