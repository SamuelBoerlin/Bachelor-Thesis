package engine.util;

import java.awt.image.BufferedImage;
import java.nio.ByteBuffer;

import org.lwjgl.BufferUtils;
import org.lwjgl.opengl.GL11;
import org.lwjgl.opengl.GL12;

/**
 * A helper class for OpenGL textures. It allows loading and rendering an OpenGL texture.
 */
public class GLTexture {
	private final int texID;
	private final int texWidth;
	private final int texHeight;

	/**
	 * Creates a new OpenGL texture from the specified image
	 * 
	 * @param image The image
	 * @param nearestNeighbor True if nearest neighbor sampling should be used, linear otherwise
	 * @param repeat True if the texture should be repeated if the UVs are not in [0, 1], clamped to
	 *        edge otherwise
	 */
	public GLTexture(BufferedImage image, boolean nearestNeighbor, boolean repeat) {
		int[] texData = new int[image.getWidth() * image.getHeight()];

		this.texWidth = image.getWidth();
		this.texHeight = image.getHeight();

		image.getRGB(0, 0, image.getWidth(), image.getHeight(), texData, 0, image.getWidth());

		ByteBuffer rgbaBuffer =
				BufferUtils.createByteBuffer(image.getWidth() * image.getHeight() * 4);

		for (int y = 0; y < this.texHeight; y++) {
			for (int x = 0; x < this.texWidth; x++) {
				int pixel = texData[y * this.texWidth + x];
				byte redComponent = (byte) ((pixel >> 16) & 0xFF);
				byte greenComponent = (byte) ((pixel >> 8) & 0xFF);
				byte blueComponent = (byte) (pixel & 0xFF);
				byte alphaComponent = (byte) ((pixel >> 24) & 0xFF);
				rgbaBuffer.put(redComponent);
				rgbaBuffer.put(greenComponent);
				rgbaBuffer.put(blueComponent);
				rgbaBuffer.put(alphaComponent);
			}
		}

		rgbaBuffer.flip();

		this.texID = GL11.glGenTextures();

		GL11.glBindTexture(GL11.GL_TEXTURE_2D, this.texID);

		if (repeat) {
			GL11.glTexParameteri(GL11.GL_TEXTURE_2D, GL11.GL_TEXTURE_WRAP_S, GL11.GL_REPEAT);
			GL11.glTexParameteri(GL11.GL_TEXTURE_2D, GL11.GL_TEXTURE_WRAP_T, GL11.GL_REPEAT);
		} else {
			GL11.glTexParameteri(GL11.GL_TEXTURE_2D, GL11.GL_TEXTURE_WRAP_S, GL12.GL_CLAMP_TO_EDGE);
			GL11.glTexParameteri(GL11.GL_TEXTURE_2D, GL11.GL_TEXTURE_WRAP_T, GL12.GL_CLAMP_TO_EDGE);
		}

		if (nearestNeighbor) {
			GL11.glTexParameteri(GL11.GL_TEXTURE_2D, GL11.GL_TEXTURE_MIN_FILTER, GL11.GL_NEAREST);
			GL11.glTexParameteri(GL11.GL_TEXTURE_2D, GL11.GL_TEXTURE_MAG_FILTER, GL11.GL_NEAREST);
		} else {
			GL11.glTexParameteri(GL11.GL_TEXTURE_2D, GL11.GL_TEXTURE_MIN_FILTER, GL11.GL_LINEAR);
			GL11.glTexParameteri(GL11.GL_TEXTURE_2D, GL11.GL_TEXTURE_MAG_FILTER, GL11.GL_LINEAR);
		}

		GL11.glTexImage2D(GL11.GL_TEXTURE_2D, 0, GL11.GL_RGBA8, image.getWidth(), image.getHeight(),
				0, GL11.GL_RGBA, GL11.GL_UNSIGNED_BYTE, rgbaBuffer);
	}

	/**
	 * Returns the width of this texture
	 */
	public final int getWidth() {
		return this.texWidth;
	}

	/**
	 * Returns the height of this texture
	 */
	public final int getHeight() {
		return this.texHeight;
	}

	/**
	 * Binds this texture
	 */
	public final void bind() {
		GL11.glBindTexture(GL11.GL_TEXTURE_2D, this.texID);
	}

	/**
	 * Returns the OpenGL texture ID of this texture
	 */
	public final int getID() {
		return this.texID;
	}

	public void destroy() {
		GL11.glDeleteTextures(this.texID);
	}
}
