package engine;

import java.nio.FloatBuffer;

import org.lwjgl.BufferUtils;
import org.lwjgl.input.Keyboard;
import org.lwjgl.input.Mouse;
import org.lwjgl.opengl.ARBShaderObjects;
import org.lwjgl.opengl.GL11;
import org.lwjgl.opengl.GL12;
import org.lwjgl.opengl.GL20;
import org.lwjgl.util.glu.GLU;
import org.lwjgl.util.vector.Matrix4f;

import engine.util.RenderUtils;


public class Renderer {
	private Engine engine;
	private Scene scene;

	private boolean wasCDown = false;
	private boolean cull = true;

	private boolean wasVDown = false;
	private boolean viewLock = false;

	private boolean wasODown = false;
	private boolean lighting = true;

	private boolean wasMDown = false;
	private boolean meshOnly = false;

	private float rotYaw;
	private float rotPitch;

	private float camX = 0.5F;
	private float camY = 0.5F;
	private float camZ = 0.5F;
	private float camDistance = 0/*2.5F*/;

	private boolean wasPDown = false;
	private boolean fixedLightPos = false;
	private float lightPosX = -10;
	private float lightPosY = 10;
	private float lightPosZ = -10;

	private static final FloatBuffer FLOAT_4_BUF = BufferUtils.createFloatBuffer(4);

	private static final FloatBuffer VIEW_MATRIX = BufferUtils.createFloatBuffer(16);
	private static final FloatBuffer INVERSE_VIEW_MATRIX = BufferUtils.createFloatBuffer(16);

	public Renderer(Engine engine) {
		this.engine = engine;
		this.scene = new Scene(engine);
	}
	
	public void render() {
		//Clear color and buffers
		GL11.glClearColor(0, 0.1f, 0.2f, 1.0f);
		GL11.glClearDepth(1);
		GL11.glClear(GL11.GL_COLOR_BUFFER_BIT | GL11.GL_DEPTH_BUFFER_BIT);

		RenderUtils.setupPerspective(0, Engine.DISPLAY_HEIGHT, Engine.DISPLAY_WIDTH, 0, 0.01f, 1000.0f);

		GL11.glPushMatrix();

		//Transform from left handed to right handed coordinate system
		GL11.glScaled(1, 1, -1);
		GL11.glEnable(GL12.GL_RESCALE_NORMAL);
		GL11.glEnable(GL11.GL_NORMALIZE);

		int dx = Mouse.getDX();
		int dy = Mouse.getDY();

		if(Mouse.isButtonDown(0) || this.viewLock) {
			this.rotYaw -= dx;
			this.rotPitch = Math.min(89.99f, Math.max(-89.99f, this.rotPitch + dy));
		}

		if(Mouse.isGrabbed() != this.viewLock) {
			Mouse.setGrabbed(this.viewLock);
		}

		double speed = Keyboard.isKeyDown(Keyboard.KEY_LCONTROL) ? 0.001 : Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) ? 0.01 : 0.1;

		if(Keyboard.isKeyDown(Keyboard.KEY_W)) {
			double yaw = Math.toRadians(this.rotYaw) + Math.PI / 2;
			double pitch = Math.toRadians(this.rotPitch);

			this.camX += Math.cos(yaw) * Math.cos(pitch) * speed;
			this.camY += Math.sin(pitch) * speed;
			this.camZ += Math.sin(yaw) * Math.cos(pitch) * speed;
		}

		if(Keyboard.isKeyDown(Keyboard.KEY_S)) {
			double yaw = Math.toRadians(this.rotYaw) + Math.PI / 2;
			double pitch = Math.toRadians(this.rotPitch);

			this.camX -= Math.cos(yaw) * Math.cos(pitch) * speed;
			this.camY -= Math.sin(pitch) * speed;
			this.camZ -= Math.sin(yaw) * Math.cos(pitch) * speed;
		}

		if(Keyboard.isKeyDown(Keyboard.KEY_A)) {
			double yaw = Math.toRadians(this.rotYaw + 90) + Math.PI / 2;

			this.camX += Math.cos(yaw) * speed;
			this.camZ += Math.sin(yaw) * speed;
		}

		if(Keyboard.isKeyDown(Keyboard.KEY_D)) {
			double yaw = Math.toRadians(this.rotYaw - 90) + Math.PI / 2;

			this.camX += Math.cos(yaw) * speed;
			this.camZ += Math.sin(yaw) * speed;
		}

		if(Keyboard.isKeyDown(Keyboard.KEY_R)) {
			this.camY += speed;
		}

		if(Keyboard.isKeyDown(Keyboard.KEY_F)) {
			this.camY -= speed;
		}

		int ds = Mouse.getDWheel();
		if(ds != 0) {
			this.camDistance = Math.max(0, this.camDistance - ds / 120.0F * 0.25F);
		}

		GL11.glTranslated(0, 0, this.camDistance/2);

		GL11.glRotatef(this.rotYaw, 0, 1, 0);
		float rx = (float) Math.cos(Math.toRadians(this.rotYaw));
		float rz = (float) Math.sin(Math.toRadians(this.rotYaw));
		GL11.glRotatef(this.rotPitch, rx, 0, rz);

		GL11.glLineWidth(2);
		GL11.glEnable(GL11.GL_LINE_SMOOTH);
		GL11.glEnable(GL11.GL_ALPHA_TEST);
		GL11.glAlphaFunc(GL11.GL_GEQUAL, 0.1F);
		GL11.glCullFace(GL11.GL_BACK);
		GL11.glFrontFace(GL11.GL_CCW);
		
		GL11.glTranslated(-this.camX, -this.camY, -this.camZ);

		GL11.glGetFloat(GL11.GL_MODELVIEW_MATRIX, VIEW_MATRIX);
		VIEW_MATRIX.rewind();

		//Render coordinate axes
		GL11.glPushMatrix();
		GL11.glTranslated(-0.5, -0.5, -0.5);

		GL11.glColor3f(0, 1, 0);
		RenderUtils.renderArrow();

		GL11.glRotated(90, 1, 0, 0);
		GL11.glColor3f(0, 0, 1);
		RenderUtils.renderArrow();

		GL11.glRotated(90, 0, 0, -1);
		GL11.glColor3f(1, 0, 0);
		RenderUtils.renderArrow();
		GL11.glPopMatrix();

		GL11.glLineWidth(1.0f);
		
		GL11.glColor3f(1, 1, 1);


		boolean cDown = Keyboard.isKeyDown(Keyboard.KEY_C);
		if(this.wasCDown != cDown && cDown) {
			this.cull = !this.cull;
		}
		this.wasCDown = cDown;

		boolean vDown = Keyboard.isKeyDown(Keyboard.KEY_V);
		if(this.wasVDown != vDown && vDown) {
			this.viewLock = !this.viewLock;
		}
		this.wasVDown = vDown;

		boolean oDown = Keyboard.isKeyDown(Keyboard.KEY_O);
		if(this.wasODown != oDown && oDown) {
			this.lighting = !this.lighting;
		}
		this.wasODown = oDown;

		boolean mDown = Keyboard.isKeyDown(Keyboard.KEY_M);
		if(this.wasMDown != mDown && mDown) {
			this.meshOnly = !this.meshOnly;
		}
		this.wasMDown = mDown;

		boolean pDown = Keyboard.isKeyDown(Keyboard.KEY_P);
		if(this.wasPDown != pDown && pDown) {
			this.fixedLightPos = !this.fixedLightPos;
			if(this.fixedLightPos) {
				this.lightPosX = this.camX;
				this.lightPosY = this.camY;
				this.lightPosZ = this.camZ;
			}
		}
		this.wasPDown = pDown;

		if(!this.fixedLightPos) {
			this.lightPosY = 10;
			this.lightPosX = (float)Math.cos((System.currentTimeMillis() % 1000000L) / 1000.0f) * 20;
			this.lightPosZ = (float)Math.sin((System.currentTimeMillis() % 1000000L) / 1000.0f) * 20;
		}

		if(this.lighting) {
			ARBShaderObjects.glUseProgramObjectARB(this.engine.getShader());
		}

		int eyeUniform = GL20.glGetUniformLocation(this.engine.getShader(), "u_eyePos");
		if(eyeUniform >= 0) {
			FLOAT_4_BUF.put(new float[] { this.camX, this.camY, this.camZ, 1 });
			FLOAT_4_BUF.flip();
			GL20.glUniform4(eyeUniform, FLOAT_4_BUF);
		}

		int lightUniform = GL20.glGetUniformLocation(this.engine.getShader(), "u_lightPos");
		if(lightUniform >= 0) {
			FLOAT_4_BUF.put(new float[] { this.lightPosX, this.lightPosY, this.lightPosZ, 1 });
			FLOAT_4_BUF.flip();
			GL20.glUniform4(lightUniform, FLOAT_4_BUF);
		}

		int viewUniform = GL20.glGetUniformLocation(this.engine.getShader(), "u_viewMatrix");
		if(viewUniform >= 0) {
			GL20.glUniformMatrix4(viewUniform, false, VIEW_MATRIX);
		}

		int inverseViewUniform = GL20.glGetUniformLocation(this.engine.getShader(), "u_inverseViewMatrix");
		if(inverseViewUniform >= 0) {
			Matrix4f viewMatrix = new Matrix4f();
			viewMatrix.load(VIEW_MATRIX);
			VIEW_MATRIX.rewind();
			viewMatrix.invert();
			viewMatrix.store(INVERSE_VIEW_MATRIX);
			INVERSE_VIEW_MATRIX.rewind();
			GL20.glUniformMatrix4(inverseViewUniform, false, INVERSE_VIEW_MATRIX);
		}


		GL11.glEnable(GL12.GL_RESCALE_NORMAL);

		GL11.glEnable(GL11.GL_BLEND);
		GL11.glBlendFunc(GL11.GL_SRC_ALPHA, GL11.GL_ONE_MINUS_SRC_ALPHA);

		if(this.meshOnly) {
			GL11.glPolygonMode(GL11.GL_FRONT_AND_BACK, GL11.GL_LINE);
		} else {
			GL11.glPolygonMode(GL11.GL_FRONT_AND_BACK, GL11.GL_FILL);
		}

		if(!this.cull) {
			GL11.glDisable(GL11.GL_CULL_FACE);
		} else {
			GL11.glEnable(GL11.GL_CULL_FACE);
		}

		this.scene.render();

		GL11.glPolygonMode(GL11.GL_FRONT_AND_BACK, GL11.GL_FILL);

		GL11.glPopMatrix();

		GL11.glFlush();

		int err = GL11.glGetError();
		if(err != GL11.GL_NO_ERROR) {
			System.err.println("GL ERROR: " + err + " " + GLU.gluErrorString(err));
		}
	}
}
