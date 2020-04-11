package engine.util;

import org.lwjgl.input.Mouse;
import org.lwjgl.opengl.Display;

import engine.Engine;

public class MouseUtils {
	public static int getMouseX() {
		int xUnscaled = Mouse.getX();
		int xScaled = (int)((double)xUnscaled / (double)Display.getWidth() * (double)Engine.DISPLAY_WIDTH);
		return xScaled;
	}
	public static int getMouseY() {
		int yUnscaled = Engine.DISPLAY_HEIGHT - Mouse.getY();
		int yScaled = (int)((double)yUnscaled / (double)Display.getHeight() * (double)Engine.DISPLAY_HEIGHT);
		return yScaled;
	}
}