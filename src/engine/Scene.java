package engine;

import static org.lwjgl.opengl.GL11.GL_COMPILE;
import static org.lwjgl.opengl.GL11.GL_FRONT;
import static org.lwjgl.opengl.GL11.GL_SHININESS;
import static org.lwjgl.opengl.GL11.GL_TRIANGLES;

import java.awt.Color;

import org.lwjgl.input.Keyboard;
import org.lwjgl.opengl.GL11;
import org.lwjgl.util.vector.Vector2f;
import org.lwjgl.util.vector.Vector3f;

import edu.mines.jtk.awt.ColorMap;
import engine.saliency.MeshUtils;
import engine.util.OBJLoader;
import engine.util.Obj;

public class Scene {
	private final Engine engine;

	private final OBJLoader modelLoader;
	private Obj model;

	private boolean wasUpDown = false;
	private boolean wasDownDown = false;
	private boolean wasLeftDown = false;
	private boolean wasRightDown = false;

	private boolean wasKDown = false;

	private float lambda = 1.0f;
	private int smoothSteps = 0;

	private int displayListId = -1;

	public Scene(Engine engine) {
		this.engine = engine;
		this.modelLoader = new OBJLoader();
		this.reloadModel();
	}

	private void reloadModel() {
		if(this.displayListId >= 0) {
			GL11.glDeleteLists(this.displayListId, 1);
		}
		this.displayListId = -1;

		this.model = this.modelLoader.loadModel(Scene.class.getResourceAsStream("/models/armadillo.obj"));

		for(int i = 0; i < this.smoothSteps; i++) {
			MeshUtils.applyDiffusion(this.model, this.lambda);
		}
	}

	public void render() {
		if(Keyboard.isKeyDown(Keyboard.KEY_UP)) {
			if(!this.wasUpDown) {
				this.smoothSteps++;
				System.out.println("Smooth steps: " + this.smoothSteps);
			}
			this.wasUpDown = true;
		} else {
			this.wasUpDown = false;
		}

		if(Keyboard.isKeyDown(Keyboard.KEY_DOWN)) {
			if(!this.wasDownDown) {
				this.smoothSteps = Math.max(0, this.smoothSteps - 1);
				System.out.println("Smooth steps: " + this.smoothSteps);
			}
			this.wasDownDown = true;
		} else {
			this.wasDownDown = false;
		}

		if(Keyboard.isKeyDown(Keyboard.KEY_RIGHT)) {
			if(!this.wasRightDown) {
				this.lambda += 0.1f;
				System.out.println("Lambda: " + this.lambda);
			}
			this.wasRightDown = true;
		} else {
			this.wasRightDown = false;
		}

		if(Keyboard.isKeyDown(Keyboard.KEY_LEFT)) {
			if(!this.wasLeftDown) {
				this.lambda = Math.max(0, this.lambda - 0.1f);
				System.out.println("Lambda: " + this.lambda);
			}
			this.wasLeftDown = true;
		} else {
			this.wasLeftDown = false;
		}

		if(Keyboard.isKeyDown(Keyboard.KEY_K)) {
			if(!this.wasKDown) {
				this.reloadModel();
			}
			this.wasKDown = true;
		} else {
			this.wasKDown = false;
		}

		GL11.glPushMatrix();

		GL11.glTranslatef(1, 1, 1);

		GL11.glFrontFace(GL11.GL_CW);

		if(this.displayListId == -1) {
			this.displayListId = GL11.glGenLists(1);
			GL11.glNewList(this.displayListId, GL_COMPILE);
			this.renderImmediate();
			GL11.glEndList();
		} else {
			GL11.glCallList(this.displayListId);
		}

		GL11.glPopMatrix();
	}

	private void renderImmediate() {
		ColorMap colorMap = new ColorMap(0, 1, ColorMap.HUE_BLUE_TO_RED);

		GL11.glMaterialf(GL_FRONT, GL_SHININESS, 120);
		GL11.glBegin(GL_TRIANGLES);
		{
			for (Obj.Face face : this.model.getFaces()) {
				Vector3f[] normals = {
						this.model.getNormals().get(face.getNormals()[0] - 1),
						this.model.getNormals().get(face.getNormals()[1] - 1),
						this.model.getNormals().get(face.getNormals()[2] - 1)
				};
				Vector2f[] texCoords = {
						this.model.getTextureCoordinates().get(face.getTextureCoords()[0] - 1),
						this.model.getTextureCoordinates().get(face.getTextureCoords()[1] - 1),
						this.model.getTextureCoordinates().get(face.getTextureCoords()[2] - 1)
				};
				Vector3f[] vertices = {
						this.model.getVertices().get(face.getVertices()[0] - 1),
						this.model.getVertices().get(face.getVertices()[1] - 1),
						this.model.getVertices().get(face.getVertices()[2] - 1)
				};

				Vector2f[] curvatures = {
						MeshUtils.principalCurvatures(this.model, face.getVertices()[0] - 1),
						MeshUtils.principalCurvatures(this.model, face.getVertices()[1] - 1),
						MeshUtils.principalCurvatures(this.model, face.getVertices()[2] - 1)
				};
				float[] meanCurvatures = {
						0.5f * (curvatures[0].x + curvatures[0].y),
						0.5f * (curvatures[1].x + curvatures[1].y),
						0.5f * (curvatures[2].x + curvatures[2].y)
				};

				float strength = 0.1f;

				{
					colorByValue(colorMap, Math.abs(meanCurvatures[0]) * strength);
					GL11.glNormal3f(normals[0].getX(), normals[0].getY(), normals[0].getZ());
					GL11.glTexCoord2f(texCoords[0].getX(), texCoords[0].getY());
					GL11.glVertex3f(vertices[0].getX(), vertices[0].getY(), vertices[0].getZ());

					colorByValue(colorMap, Math.abs(meanCurvatures[1]) * strength);
					GL11.glNormal3f(normals[1].getX(), normals[1].getY(), normals[1].getZ());
					GL11.glTexCoord2f(texCoords[1].getX(), texCoords[1].getY());
					GL11.glVertex3f(vertices[1].getX(), vertices[1].getY(), vertices[1].getZ());

					colorByValue(colorMap, Math.abs(meanCurvatures[2]) * strength);
					GL11.glNormal3f(normals[2].getX(), normals[2].getY(), normals[2].getZ());
					GL11.glTexCoord2f(texCoords[2].getX(), texCoords[2].getY());
					GL11.glVertex3f(vertices[2].getX(), vertices[2].getY(), vertices[2].getZ());
				}
			}
		}
		GL11.glEnd();
	}

	private static void colorByValue(ColorMap colorMap, float value) {
		Color color = colorMap.getColor(Math.max(0, Math.min(1, value)));
		GL11.glColor3f(color.getRed() / 255.0f, color.getGreen() / 255.0f, color.getBlue() / 255.0f);
	}
}
