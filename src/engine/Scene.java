package engine;

import static org.lwjgl.opengl.GL11.GL_COMPILE;
import static org.lwjgl.opengl.GL11.GL_FRONT;
import static org.lwjgl.opengl.GL11.GL_SHININESS;
import static org.lwjgl.opengl.GL11.GL_TRIANGLES;

import java.awt.Color;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Random;

import org.lwjgl.input.Keyboard;
import org.lwjgl.opengl.ARBShaderObjects;
import org.lwjgl.opengl.GL11;
import org.lwjgl.opengl.GL13;
import org.lwjgl.opengl.GL20;
import org.lwjgl.util.vector.Vector2f;
import org.lwjgl.util.vector.Vector3f;

import edu.mines.jtk.awt.ColorMap;
import engine.saliency.FeatureSample;
import engine.saliency.MeshUtils;
import engine.saliency.MeshUtils.Score;
import engine.util.OBJLoader;
import engine.util.Obj;
import engine.util.Obj.Face;

public class Scene {
	private final Engine engine;

	private final OBJLoader modelLoader;
	private Obj[] models;
	private Map<Integer, List<Face>> faceMap;

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

		String modelFile = "/models/cat.obj";
		
		this.models = new Obj[3];
		for(int i = 0; i < 3; i++) {
			long time = System.currentTimeMillis();
			
			Obj model = this.modelLoader.loadModel(Scene.class.getResourceAsStream(modelFile));
			
			if(i == 0) {
				this.faceMap = MeshUtils.mapFaces(model, new HashMap<>());
			}
			
			int subdivs = (int)Math.ceil(this.lambda);
			
			for(int j = 0; j < i * subdivs; j++) {
				MeshUtils.applyDiffusion(model, this.faceMap, this.lambda / subdivs);
			}
			
			//Scale model to have a surface area of 100
			float surfaceArea = 0.0f;
			for(Face face : model.getFaces()) {
				surfaceArea += MeshUtils.faceCross(model, face).length() * 0.5f;
			}
			float scale = (float)Math.pow(100.0f / surfaceArea, 0.5f);
			for(Vector3f vertex : model.getVertices()) {
				vertex.x *= scale;
				vertex.y *= scale;
				vertex.z *= scale;
			}
			
			System.out.println("Loaded model with " + i + " smoothing steps in " + (System.currentTimeMillis() - time) + "ms");
			
			this.models[i] = model;
		}
	}

	public void render() {
		if(Keyboard.isKeyDown(Keyboard.KEY_UP)) {
			if(!this.wasUpDown) {
				this.smoothSteps = Math.min(this.models.length - 1, this.smoothSteps + 1);
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

		float totalCurvature = 0.0f;

		for(int i = 0; i < this.models[0].getVertices().size(); i++) {
			Vector2f curv = MeshUtils.principalCurvatures(this.models[0], this.faceMap, i);

			List<Obj.Face> sharedFaces = faceMap.get(i);

			if(sharedFaces != null) {
				float area = 0.0f;
				for(Obj.Face face : sharedFaces) {
					area += MeshUtils.faceCross(this.models[0], face).length() * 0.5f;
				}

				float gaussianCurvature = curv.x * curv.y;
				if(Float.isFinite(gaussianCurvature) && Float.isFinite(area)) {
					totalCurvature += area / 3.0f * Math.abs(gaussianCurvature);
				}
			}
		}

		System.out.println("Total curvature: " + totalCurvature);

		float a = 450.0f;
		float b = 2.0f;
		float c = 0.1f;
		
		int numSamples = (int)Math.max(2000 * Math.pow(Math.max((totalCurvature - a) / b, 0), c), 2000);
		
		System.out.println("Number of samples: " + numSamples);

		Score score = new Score() {

			@Override
			public float score(Face face, float u, float v, Vector3f position) {
				float[] meanAbsCurvatures = new float[3];

				for(int i = 0; i < 3; i++) {
					Vector2f[] principalCurvatures = {
							MeshUtils.principalCurvatures(models[i], faceMap, face.getVertices()[0] - 1),
							MeshUtils.principalCurvatures(models[i], faceMap, face.getVertices()[1] - 1),
							MeshUtils.principalCurvatures(models[i], faceMap, face.getVertices()[2] - 1)
					};
					
					//Interpolate curvature
					float kappa1 = u * principalCurvatures[1].x + v * principalCurvatures[2].x + (1 - u - v) * principalCurvatures[0].x;
					float kappa2 = u * principalCurvatures[1].y + v * principalCurvatures[2].y + (1 - u - v) * principalCurvatures[0].y;
					
					meanAbsCurvatures[i] = 0.5f * (kappa1 + kappa2);
				}
				
				//TODO Is this right? Direct sum etc.
				return (meanAbsCurvatures[2] - meanAbsCurvatures[1]) * (meanAbsCurvatures[1] - meanAbsCurvatures[0]);
			}
		};
		
		List<FeatureSample> samples = MeshUtils.sampleFeatures(this.models[this.smoothSteps], score, new ArrayList<>(), numSamples, new Random());

		Collections.sort(samples, (s1, s2) -> -Float.compare(s1.score, s2.score));
		
		//Remove bottom 90% of samples
		for(int i = samples.size() - 1; i > numSamples / 10; i--) {
			samples.remove(i);
		}
		
		float colorStrength = 0.1f;

		GL11.glEnable(GL11.GL_POLYGON_OFFSET_FILL);
		GL11.glPolygonOffset(10, 1000);
		
		GL11.glMaterialf(GL_FRONT, GL_SHININESS, 120);
		GL11.glBegin(GL_TRIANGLES);
		{
			for (Obj.Face face : this.models[this.smoothSteps].getFaces()) {
				/*Vector3f[] normals = {
						this.models[this.smoothSteps].getNormals().get(face.getNormals()[0] - 1),
						this.models[this.smoothSteps].getNormals().get(face.getNormals()[1] - 1),
						this.models[this.smoothSteps].getNormals().get(face.getNormals()[2] - 1)
				};*/
				/*Vector2f[] texCoords = {
						this.model.getTextureCoordinates().get(face.getTextureCoords()[0] - 1),
						this.model.getTextureCoordinates().get(face.getTextureCoords()[1] - 1),
						this.model.getTextureCoordinates().get(face.getTextureCoords()[2] - 1)
				};*/
				Vector3f[] vertices = {
						this.models[this.smoothSteps].getVertices().get(face.getVertices()[0] - 1),
						this.models[this.smoothSteps].getVertices().get(face.getVertices()[1] - 1),
						this.models[this.smoothSteps].getVertices().get(face.getVertices()[2] - 1)
				};

				Vector2f[] curvatures = {
						MeshUtils.principalCurvatures(this.models[this.smoothSteps], this.faceMap, face.getVertices()[0] - 1),
						MeshUtils.principalCurvatures(this.models[this.smoothSteps], this.faceMap, face.getVertices()[1] - 1),
						MeshUtils.principalCurvatures(this.models[this.smoothSteps], this.faceMap, face.getVertices()[2] - 1)
				};
				float[] meanCurvatures = {
						0.5f * (curvatures[0].x + curvatures[0].y),
						0.5f * (curvatures[1].x + curvatures[1].y),
						0.5f * (curvatures[2].x + curvatures[2].y)
				};

				{
					colorByValue(colorMap, Math.abs(meanCurvatures[0]) * colorStrength);
					//GL11.glNormal3f(normals[0].getX(), normals[0].getY(), normals[0].getZ());
					//GL11.glTexCoord2f(texCoords[0].getX(), texCoords[0].getY());
					GL11.glVertex3f(vertices[0].getX(), vertices[0].getY(), vertices[0].getZ());

					colorByValue(colorMap, Math.abs(meanCurvatures[1]) * colorStrength);
					//GL11.glNormal3f(normals[1].getX(), normals[1].getY(), normals[1].getZ());
					//GL11.glTexCoord2f(texCoords[1].getX(), texCoords[1].getY());
					GL11.glVertex3f(vertices[1].getX(), vertices[1].getY(), vertices[1].getZ());

					colorByValue(colorMap, Math.abs(meanCurvatures[2]) * colorStrength);
					//GL11.glNormal3f(normals[2].getX(), normals[2].getY(), normals[2].getZ());
					//GL11.glTexCoord2f(texCoords[2].getX(), texCoords[2].getY());
					GL11.glVertex3f(vertices[2].getX(), vertices[2].getY(), vertices[2].getZ());
				}
			}
		}
		GL11.glEnd();
		
		GL11.glDisable(GL11.GL_POLYGON_OFFSET_FILL);
		
		ARBShaderObjects.glUseProgramObjectARB(0);
		
		GL11.glEnable(GL11.GL_POINT_SMOOTH);
		
		GL11.glPointSize(11.0f);
		GL11.glBegin(GL11.GL_POINTS);
		{
			for(FeatureSample sample : samples) {
				GL11.glColor3f(0, 0, 0);
				GL11.glVertex3f(sample.position.x, sample.position.y, sample.position.z);
			}
		}	
		GL11.glEnd();
		
		GL11.glPointSize(8.0f);
		GL11.glBegin(GL11.GL_POINTS);
		{
			for(FeatureSample sample : samples) {
				colorByValue(colorMap, Math.abs(sample.score) * colorStrength);
				GL11.glVertex3f(sample.position.x, sample.position.y, sample.position.z);
			}
		}	
		GL11.glEnd();
	}

	private static void colorByValue(ColorMap colorMap, float value) {
		Color color = colorMap.getColor(Math.max(0, Math.min(1, value)));
		GL11.glColor3f(color.getRed() / 255.0f, color.getGreen() / 255.0f, color.getBlue() / 255.0f);
	}
}
