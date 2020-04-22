package engine.saliency;

import org.lwjgl.util.vector.Vector3f;

import engine.util.Obj.Face;

public class DifferenceOfLaplacian implements MeshUtils.SaliencyMapper {
	private final float[][] scores;

	public DifferenceOfLaplacian(float[][] scores) {
		this.scores = scores;
	}

	public static float scoreCurvatures(float meanCurvature3, float meanCurvature2, float meanCurvature1, float meanCurvature0) {
		//TODO Is this right? Direct sum etc.
		return ((meanCurvature3 - meanCurvature2) * (meanCurvature2 - meanCurvature1));
	}

	@Override
	public float map(Face face, float u, float v, Vector3f position) {
		float[] meanCurvatures = new float[4];

		for(int i = 0; i < 4; i++) {
			meanCurvatures[i] =
					this.scores[i][face.getVertices()[1] - 1] * u +
					this.scores[i][face.getVertices()[2] - 1] * v +
					this.scores[i][face.getVertices()[0] - 1] * (1 - u - v);
		}

		return scoreCurvatures(meanCurvatures[3], meanCurvatures[2], meanCurvatures[1], meanCurvatures[0]);
	}
}
