package engine.saliency;

import org.lwjgl.util.vector.Vector3f;

public class FeatureSample {
	public final Vector3f position;
	public final float score;

	public FeatureSample(Vector3f position, float score) {
		this.position = position;
		this.score = score;
	}
}
