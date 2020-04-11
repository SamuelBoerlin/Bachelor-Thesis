package engine.util;

public class Vertex2D {
	public final double x, y;
	public final int color;
	public Vertex2D(double x, double y, int color) {
		this.x = x;
		this.y = y;
		this.color = color;
	}
	public float[] getRGBA() {
		float rgba[] = new float[4];
		rgba[0] = (this.color >> 16 & 0xFF) / 255.0F;
		rgba[1] = (this.color >> 8 & 0xFF) / 255.0F;
		rgba[2] = (this.color & 0xFF) / 255.0F;
		rgba[3] = (this.color >> 24 & 0xFF) / 255.0F;
		return rgba;
	}
}
