package engine.saliency;

import org.lwjgl.util.vector.Vector3f;

import engine.saliency.IsodataClustering.Cluster;
import engine.saliency.MeshUtils.ColorMapper.ColorCode;

public class FeatureSample {
	public final Vector3f position;
	public final float saliency;
	public final ColorCode color;

	private Cluster cluster;
	
	public FeatureSample(Vector3f position, float saliency, ColorCode color) {
		this.position = position;
		this.saliency = saliency;
		this.color = color;
	}
	
	public void setCluster(Cluster cluster) {
		this.cluster = cluster;
	}
	
	public Cluster getCluster() {
		return this.cluster;
	}
}
