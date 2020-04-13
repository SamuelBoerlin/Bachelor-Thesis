package engine.saliency;

import org.lwjgl.util.vector.Vector3f;

import engine.saliency.IsodataClustering.Cluster;

public class FeatureSample {
	public final Vector3f position;
	public final float score;

	private Cluster cluster;
	
	public FeatureSample(Vector3f position, float score) {
		this.position = position;
		this.score = score;
	}
	
	public void setCluster(Cluster cluster) {
		this.cluster = cluster;
	}
	
	public Cluster getCluster() {
		return this.cluster;
	}
}
