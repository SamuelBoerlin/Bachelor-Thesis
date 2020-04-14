package engine.saliency;

import java.awt.Color;
import java.awt.image.BufferedImage;

import org.lwjgl.util.vector.Vector2f;
import org.lwjgl.util.vector.Vector3f;

import engine.util.Obj;
import engine.util.Obj.Face;

public class TextureColorMapper implements MeshUtils.ColorMapper {
	private final Obj model;
	private final BufferedImage texture;

	public TextureColorMapper(Obj obj, BufferedImage texture) {
		this.model = obj;
		this.texture = texture;
	}

	public static Color sampleColor(Obj model, Face face, float u, float v, BufferedImage image) {
		Vector2f texUv0 = model.getTextureCoordinates().get(face.getTextureCoords()[0] - 1);
		Vector2f texUv1 = model.getTextureCoordinates().get(face.getTextureCoords()[1] - 1);
		Vector2f texUv2 = model.getTextureCoordinates().get(face.getTextureCoords()[2] - 1);

		float texU = texUv1.x * u + texUv2.x * v + texUv0.x * (1 - u - v);
		float texV = 1 - (texUv1.y * u + texUv2.y * v + texUv0.y * (1 - u - v));

		int px = Math.max(0, Math.min(image.getWidth(), (int)Math.floor(texU * image.getWidth())));
		int py = Math.max(0, Math.min(image.getHeight(), (int)Math.floor(texV * image.getHeight())));

		//TODO Nearest neighbor or interpolate?
		int rgb = image.getRGB(px, py);

		return new Color((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF, (rgb >> 24) & 0xFF);
	}

	@Override
	public Color map(Face face, float u, float v, Vector3f position) {
		return sampleColor(this.model, face, u, v, this.texture);
	}
}
