package engine;
import java.io.InputStream;
import java.util.Scanner;

import org.lwjgl.input.Keyboard;
import org.lwjgl.opengl.ARBFragmentShader;
import org.lwjgl.opengl.ARBShaderObjects;
import org.lwjgl.opengl.ARBVertexShader;
import org.lwjgl.opengl.Display;
import org.lwjgl.opengl.DisplayMode;
import org.lwjgl.opengl.GL11;
import org.lwjgl.opengl.PixelFormat;

public class Engine {
	public static final int DISPLAY_WIDTH = 1200;
	public static final int DISPLAY_HEIGHT = (int)(DISPLAY_WIDTH/(float)16*9);

	private boolean running = true;

	private Renderer renderer;

	private int shader;
	
	private long frameTimer = System.currentTimeMillis();
	private long frameStartTime = System.nanoTime();
	private int frameCounter = 0;

	public void run() {
		try {
			Display.setDisplayMode(new DisplayMode(DISPLAY_WIDTH, DISPLAY_HEIGHT));
			Display.setTitle("Mesh Feature Extraction");
			Display.create((new PixelFormat()).withDepthBits(24));
		} catch(Exception ex) {
			ex.printStackTrace();
			System.exit(0);
		}

		this.init();

		while(this.running) {
			this.gameLoop();

			Display.update();
			Display.sync(60);
		}
	}

	public void gameLoop() {
		if (Display.isCreated() && (Display.isCloseRequested() || Keyboard.isKeyDown(Keyboard.KEY_ESCAPE))) {
			this.running = false;
		}
		try {
			if(this.frameCounter != 0 && System.currentTimeMillis() - this.frameTimer > 1000) {
				//System.out.println("Frame time: " + (System.nanoTime() - this.frameStartTime) / 1000000.0f / this.frameCounter + "ms");
				this.frameStartTime = System.nanoTime();
				this.frameCounter = 0;
				this.frameTimer = System.currentTimeMillis();
			}
			
			this.renderer.render();
			this.frameCounter++;
		} catch(Exception ex) {
			ex.printStackTrace();
		}
	}

	public void init() {
		this.renderer = new Renderer(this);
		
		int vertShader = 0, fragShader = 0;

		try {
			vertShader = createShader("/shaders/world.vert", ARBVertexShader.GL_VERTEX_SHADER_ARB);
		} catch(Exception exc) {
			throw new RuntimeException("Failed creating vertex shaders", exc);
		}
		try {
			fragShader = createShader("/shaders/world.frag", ARBFragmentShader.GL_FRAGMENT_SHADER_ARB);
		} catch(Exception exc) {
			throw new RuntimeException("Failed creating fragment shaders", exc);
		}

		this.shader = ARBShaderObjects.glCreateProgramObjectARB();

		if(this.shader == 0) {
			throw new RuntimeException("Failed creating shader");
		}

		ARBShaderObjects.glAttachObjectARB(this.shader, vertShader);
		ARBShaderObjects.glAttachObjectARB(this.shader, fragShader);

		ARBShaderObjects.glLinkProgramARB(this.shader);
		if(ARBShaderObjects.glGetObjectParameteriARB(this.shader, ARBShaderObjects.GL_OBJECT_LINK_STATUS_ARB) == GL11.GL_FALSE) {
			throw new RuntimeException("Failed linking shader\n" + getLogInfo(this.shader));
		}

		ARBShaderObjects.glValidateProgramARB(this.shader);
		if(ARBShaderObjects.glGetObjectParameteriARB(this.shader, ARBShaderObjects.GL_OBJECT_VALIDATE_STATUS_ARB) == GL11.GL_FALSE) {
			throw new RuntimeException("Failed validating shader\n" + getLogInfo(this.shader));
		}
	}

	public int getShader() {
		return this.shader;
	}
	
	public Renderer getRenderer() {
		return this.renderer;
	}

	private int createShader(String filename, int shaderType) throws Exception {
		int shader = 0;
		try {
			shader = ARBShaderObjects.glCreateShaderObjectARB(shaderType);

			if(shader == 0) {
				return 0;
			}

			ARBShaderObjects.glShaderSourceARB(shader, readFileAsString(filename));
			ARBShaderObjects.glCompileShaderARB(shader);

			if (ARBShaderObjects.glGetObjectParameteriARB(shader, ARBShaderObjects.GL_OBJECT_COMPILE_STATUS_ARB) == GL11.GL_FALSE) {
				throw new RuntimeException("Error creating shader: " + getLogInfo(shader));
			}

			return shader;
		} catch(Exception exc) {
			ARBShaderObjects.glDeleteObjectARB(shader);
			throw exc;
		}
	}

	private static String getLogInfo(int obj) {
		return ARBShaderObjects.glGetInfoLogARB(obj, ARBShaderObjects.glGetObjectParameteriARB(obj, ARBShaderObjects.GL_OBJECT_INFO_LOG_LENGTH_ARB));
	}

	private String readFileAsString(String file) throws Exception {
		try(InputStream stream = Engine.class.getResourceAsStream(file); Scanner scanner = new Scanner(stream, "UTF-8")) {
			return scanner.useDelimiter("\\A").next();
		}
	}
}