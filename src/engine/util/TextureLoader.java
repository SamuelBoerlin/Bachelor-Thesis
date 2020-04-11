package engine.util;

import java.awt.image.BufferedImage;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;

import javax.imageio.ImageIO;

public class TextureLoader {
	public static BufferedImage loadImage(String file) throws IOException, FileNotFoundException {
		try (InputStream is = TextureLoader.class.getResourceAsStream("/textures/" + file)) {
			if (is != null) {
				BufferedImage image = ImageIO.read(is);
				if (image != null) {
					return image;
				}
				throw new IOException("Could not load texture '" + file + "'");
			}
			throw new FileNotFoundException("Internal texture '" + file + "' could not be found");
		}
	}
}
