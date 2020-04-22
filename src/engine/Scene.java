package engine;

import static org.lwjgl.opengl.GL11.GL_COMPILE;
import static org.lwjgl.opengl.GL11.GL_FRONT;
import static org.lwjgl.opengl.GL11.GL_SHININESS;
import static org.lwjgl.opengl.GL11.GL_TRIANGLES;

import java.awt.Color;
import java.awt.image.BufferedImage;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;
import java.util.Random;

import javax.imageio.ImageIO;

import org.apache.commons.math3.analysis.interpolation.SplineInterpolator;
import org.apache.commons.math3.analysis.polynomials.PolynomialSplineFunction;
import org.lwjgl.input.Keyboard;
import org.lwjgl.opengl.ARBShaderObjects;
import org.lwjgl.opengl.GL11;
import org.lwjgl.opengl.GL13;
import org.lwjgl.opengl.GL20;
import org.lwjgl.util.vector.Vector2f;
import org.lwjgl.util.vector.Vector3f;

import edu.mines.jtk.awt.ColorMap;
import engine.saliency.DifferenceOfLaplacian;
import engine.saliency.FeatureSample;
import engine.saliency.IsodataClustering;
import engine.saliency.IsodataClustering.Cluster;
import engine.saliency.MeshUtils;
import engine.saliency.MeshUtils.ColorMapper;
import engine.saliency.MeshUtils.FaceList;
import engine.saliency.MeshUtils.SaliencyMapper;
import engine.saliency.TextureColorMapper;
import engine.util.GLTexture;
import engine.util.OBJLoader;
import engine.util.Obj;
import engine.util.Obj.Face;

public class Scene {
	private final Engine engine;

	private final OBJLoader modelLoader;
	private Obj[] models;
	private float[][] scores;
	private FaceList[] faceMap;

	private boolean wasUpDown = false;
	private boolean wasDownDown = false;
	private boolean wasLeftDown = false;
	private boolean wasRightDown = false;

	private boolean wasKDown = false;

	private float lambda = 8.0f;
	private int smoothSteps = 0;

	private int displayListId = -1;

	private String modelFile = "/models/penguin.obj";
	private String textureFile = "/models/penguin.png";

	private GLTexture texture = null;

	private SaliencyMapper saliencyMapper;
	private ColorMapper colorMapper;

	private int[] prevFeature = null;
	private int[] feature = null;

	public Scene(Engine engine) {
		this.engine = engine;
		this.modelLoader = new OBJLoader();
		this.reloadModel();
	}

	private void reloadModel() {
		if(this.feature != null) {
			this.prevFeature = this.feature.clone();
		}
		this.feature = null;

		if(this.displayListId >= 0) {
			GL11.glDeleteLists(this.displayListId, 1);
		}
		this.displayListId = -1;

		if(this.texture != null) {
			this.texture.destroy();
			this.texture = null;
		}

		BufferedImage image = null;
		if(this.textureFile != null) {
			try {
				image = ImageIO.read(Scene.class.getResourceAsStream(this.textureFile));
				this.texture = new GLTexture(image, false, false);
			} catch (IOException e) {
				e.printStackTrace();
			}
		}

		this.models = new Obj[4];
		this.scores = new float[4][];
		for(int i = 0; i < 4; i++) {
			long time = System.currentTimeMillis();

			Obj model = this.modelLoader.loadModel(Scene.class.getResourceAsStream(this.modelFile));

			if(i == 0) {
				this.faceMap = MeshUtils.mapFaces(model);
			}

			int subdivs = (int)Math.ceil(this.lambda);

			//TODO Reuse vertices and scores from previous smoothing stage

			//Apply laplacian smoothing to vertices
			for(int j = 0; j < i * subdivs; j++) {
				MeshUtils.applyVertexDiffusion(model, this.faceMap, this.lambda / subdivs);
			}

			//Scale model to have a surface area of 100
			MeshUtils.scaleSurface(model, 100.0f);

			//Calculate mean curvatures
			int numVertices = model.getVertices().size();
			float[] scores = new float[numVertices];
			for(int vertexIndex = 0; vertexIndex < numVertices; vertexIndex++) {
				Vector2f principalCurvatures = MeshUtils.principalCurvatures(model, this.faceMap, vertexIndex);
				scores[vertexIndex] = 0.5f * (principalCurvatures.x + principalCurvatures.y);
			}

			//Apply laplacian smoothing to curvatures
			for(int j = 0; j < i * subdivs; j++) {
				MeshUtils.applyScalarDiffusion(model, this.faceMap, this.lambda / subdivs, scores);
			}

			System.out.println("Loaded model with " + i + " smoothing steps in " + (System.currentTimeMillis() - time) + "ms");

			this.models[i] = model;
			this.scores[i] = scores;
		}

		this.saliencyMapper = new DifferenceOfLaplacian(this.scores);
		if(image != null) {
			this.colorMapper = new TextureColorMapper(this.models[0], image);
		} else {
			this.colorMapper = new MeshUtils.ColorMapper() {
				@Override
				public ColorCode map(Face face, float u, float v, Vector3f position) {
					return new ColorCode(Color.WHITE, 0);
				}
			};
		}
	}

	public void render() {
		if(Keyboard.isKeyDown(Keyboard.KEY_UP)) {
			if(!this.wasUpDown) {
				this.smoothSteps = Math.min(this.models.length- 1, this.smoothSteps + 1);
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
			if(this.engine.getRenderer().isShaded()) {
				if(this.texture != null) {
					int texUniform = GL20.glGetUniformLocation(this.engine.getShader(), "u_texture");
					if(texUniform >= 0) {
						GL13.glActiveTexture(GL13.GL_TEXTURE0);
						GL11.glBindTexture(GL11.GL_TEXTURE_2D, this.texture.getID());
						GL20.glUniform1i(texUniform, 0);
					}
				}

				int saliencyUniform = GL20.glGetUniformLocation(this.engine.getShader(), "u_saliencyOverlay");
				if(saliencyUniform >= 0) {
					if(this.texture != null) {
						GL20.glUniform1f(saliencyUniform, (float)(Math.sin((System.currentTimeMillis() % 100000) * 0.001f) + 1) * 0.5f);
					} else {

						GL20.glUniform1f(saliencyUniform, 1.0f);
					}
				}
			}

			GL11.glCallList(this.displayListId);
		}

		GL11.glFrontFace(GL11.GL_CCW);

		if(this.feature != null) {
			int max = 0;
			for(int value : this.feature) {
				max = Math.max(value, max);
			}

			float sum = 0;
			for(int value : this.feature) {
				sum += value;
			}

			GL11.glMatrixMode(GL11.GL_PROJECTION);
			GL11.glLoadIdentity();
			GL11.glOrtho(0, 1000, 0, 1000, -100, 100);
			GL11.glMatrixMode(GL11.GL_MODELVIEW);
			GL11.glLoadIdentity();

			GL11.glDisable(GL11.GL_TEXTURE_2D);
			GL11.glColor4f(1, 1, 1, 1);

			GL11.glBegin(GL11.GL_QUADS);
			for(int i = 0; i < this.feature.length; i++) {
				int value = this.feature[i];

				float height = value / (float)max * 300.0f;

				float x = i * 11 + 100;
				float y = 100;

				GL11.glVertex2f(x, y);
				GL11.glVertex2f(x + 10, y);
				GL11.glVertex2f(x + 10, y + height);
				GL11.glVertex2f(x, y + height);
			}
			GL11.glEnd();

			SplineInterpolator interpolator = new SplineInterpolator();

			double[] splineX = new double[this.feature.length];
			double[] splineY = new double[this.feature.length];
			for(int i = 0; i < this.feature.length; i++) {
				splineX[i] = i / (float)(this.feature.length - 1);
				splineY[i] = this.feature[i] / (float)max;
			}

			GL11.glLineWidth(2.0f);
			GL11.glColor4f(1, 0, 0, 1);

			PolynomialSplineFunction spline = interpolator.interpolate(splineX, splineY);
			PolynomialSplineFunction splined1 = spline.polynomialSplineDerivative();
			PolynomialSplineFunction splined2 = splined1.polynomialSplineDerivative();

			double[] spline2knots = splined2.getKnots();

			int subdivs = 20;

			GL11.glBegin(GL11.GL_LINE_STRIP);
			for(int i = 0; i < (this.feature.length - 1) * subdivs; i++) {
				float ix = (i) / (float)((this.feature.length - 1) * subdivs - 1);
				float iy = (float)spline.value(ix);

				float height = iy * 300.0f;

				float x = (i / (float)subdivs + 0.5f) * 11 + 100;
				float y = 100;

				GL11.glVertex2f(x, y + height);
			}
			GL11.glEnd();

			GL11.glBegin(GL11.GL_LINE_STRIP);
			for(int i = 0; i < (this.feature.length - 1) * subdivs; i++) {
				float ix = i / (float)((this.feature.length - 1) * subdivs - 1);
				float iy = (float)splined1.value(ix);

				float height = iy;

				float x = (i / (float)subdivs + 0.5f) * 11 + 100;
				float y = 100;

				GL11.glVertex2f(x, y + height);
			}
			GL11.glEnd();

			GL11.glBegin(GL11.GL_LINE_STRIP);
			for(int i = 0; i < (this.feature.length - 1) * subdivs; i++) {
				float ix = i / (float)((this.feature.length - 1) * subdivs - 1);
				float iy = (float)(splined2.value(ix));

				float height = iy;

				float x = (i / (float)subdivs + 0.5f) * 11 + 100;
				float y = 100;

				GL11.glVertex2f(x, y + height);
			}
			GL11.glEnd();

			GL11.glColor4f(0, 1, 0, 1);

			GL11.glBegin(GL11.GL_LINE_STRIP);
			for(int i = 0; i < (this.feature.length - 1) * subdivs; i++) {
				float ix = i / (float)((this.feature.length - 1) * subdivs - 1);

				float height = (float)this.estimateDensity(this.feature, 1.0f / sum, this.estimateBandwidth(this.splineDeriv2(this.feature, 1.0f / sum)), ix) * 150.0f;

				float x = (i / (float)subdivs + 0.5f) * 11 + 100;
				float y = 100;

				GL11.glVertex2f(x, y + height);
			}
			GL11.glEnd();

			GL11.glLineWidth(1.0f);
			GL11.glColor4f(0, 0, 0, 1);

			GL11.glBegin(GL11.GL_LINES);
			for(int i = 0; i < spline2knots.length; i++) {
				float x = ((float)(spline2knots[i]) * (this.feature.length - 1) + 0.5f) * 11 + 100;
				float y = 100;

				GL11.glVertex2f(x, y + 300);
				GL11.glVertex2f(x, y);
			}
			GL11.glEnd();

			GL11.glColor4f(1, 1, 1, 1);
		}


		GL11.glPopMatrix();
	}

	private float calculateSimilarity(int[] feature1, int[] feature2) {
		int sum1 = 0;
		for(int value : feature1) {
			sum1 += value;
		}

		int sum2 = 0;
		for(int value : feature2) {
			sum2 += value;
		}

		int sum3 = (int) Math.ceil(0.5f * (sum1 + sum2));
		int[] feature3 = new int[feature1.length];
		for(int i = 0; i < feature1.length; i++) {
			feature3[i] = (int) Math.ceil(0.5f * (feature1[i] + feature2[i]));
		}

		PolynomialSplineFunction s1 = this.splineDeriv2(feature1, 1.0f / sum1);
		PolynomialSplineFunction s2 = this.splineDeriv2(feature2, 1.0f / sum2);
		PolynomialSplineFunction s3 = this.splineDeriv2(feature3, 1.0f / sum3);

		float bw1 = this.estimateBandwidth(s1);
		float bw2 = this.estimateBandwidth(s2);
		float bw3 = this.estimateBandwidth(s3);

		int samples = 1000;

		double dkl13 = 0.0;
		for(int i = 0; i < samples; i++) {
			float x1 = i / (float)(samples - 1);
			float x2 = (i + 1) / (float)(samples - 1);

			float dx = x2 - x1;
			float mid = 0.5f * (x1 + x2);

			double px = this.estimateDensity(feature1, 1.0f / sum1, bw1, mid);
			double qx = this.estimateDensity(feature3, 1.0f / sum3, bw3, mid);

			dkl13 -= px * Math.log(px / qx) * dx;
		}

		double dkl23 = 0.0;
		for(int i = 0; i < samples; i++) {
			float x1 = i / (float)(samples - 1);
			float x2 = (i + 1) / (float)(samples - 1);

			float dx = x2 - x1;
			float mid = 0.5f * (x1 + x2);

			double px = this.estimateDensity(feature2, 1.0f / sum2, bw2, mid);
			double qx = this.estimateDensity(feature3, 1.0f / sum3, bw3, mid);

			dkl23 -= px * Math.log(px / qx) * dx;
		}

		return 1 + (float)(0.5f * dkl13 + 0.5f * dkl23);
	}

	private double estimateDensity(int[] feature, float scale, double bandwidth, float x) {
		double density = 0.0;
		float numSamples = 0;

		for(int i = 0; i < feature.length; i++) {
			float sx = i / (float)(feature.length - 1);
			float samplesForValue = feature[i] * scale;

			double arg = (x - sx) / (double)bandwidth;
			density += 1 / Math.sqrt(2 * Math.PI) * Math.exp(-0.5D * arg * arg) * samplesForValue;
			numSamples += samplesForValue;
		}

		return density / (numSamples * bandwidth);
	}

	private PolynomialSplineFunction splineDeriv2(int[] feature, float scale) {
		SplineInterpolator interpolator = new SplineInterpolator();

		double[] sx = new double[feature.length];
		double[] sy = new double[feature.length];
		for(int i = 0; i < feature.length; i++) {
			sx[i] = i / (float)(feature.length - 1);
			sy[i] = feature[i] * scale;
		}

		return interpolator.interpolate(sx, sy).polynomialSplineDerivative().polynomialSplineDerivative();
	}

	private float estimateBandwidth(PolynomialSplineFunction c) {
		double[] knots = c.getKnots();

		//Second derivative of cubic polynomial is linear,
		//so only the end points of the linear segments need to be sampled and the integrals
		//between can be calculated analytically
		double integralcsq = 0.0;
		for(int i = 0;  i < knots.length - 1; i++) {
			float x1 = (float)knots[i];
			float x2 = (float)knots[i + 1];
			float y1 = (float)c.value(x1);
			float y2 = (float)c.value(x2);

			//Calculate linear segment parameters
			float dx = x2 - x1;
			float a = (y2 - y1) / dx;
			float b = y1;

			if(a < 0.000000001f) {
				integralcsq += dx * b * b;
			} else {
				//Integral of linear segment squared (a*x + b)^2
				float n1 = b;
				float n2 = a * dx + b;
				float piece = (n2 * n2 * n2 - n1 * n1 * n1) / (3.0f * a);

				//Add to total integral
				integralcsq += piece;
			}
		}

		double rk = 0.282095; //Integral of gaussian with variance=1, mean=0, from -inf to +inf = 1 / (2 * sqrt(pi))
		double mu22 = 1.0; //Integral of c * x^2 * exp(-a*x^2), from -inf to +inf = c * sqrt(pi) / (2 * a^(3/2)). With c=1/sqrt(pi), a=1/2 --> integral = 1

		int n = 20;
		
		//Bandwidth estimation
		float h = (float)(Math.pow(rk / (mu22 * integralcsq), 1 / 5.0) * Math.pow(n, -1 / 5.0));

		return h;
	}

	private void renderImmediate() {
		ColorMap colorMap = new ColorMap(0, 1, ColorMap.HUE_BLUE_TO_RED);

		float totalCurvature = MeshUtils.totalAbsoluteGaussianCurvature(this.models[0], this.faceMap);

		System.out.println("Total curvature: " + totalCurvature);

		float a = 450.0f;
		float b = 2.0f;
		float c = 0.1f;

		int numSamples = (int)Math.max(2000 * Math.pow(Math.max((totalCurvature - a) / b, 0), c), 2000);

		System.out.println("Number of samples: " + numSamples);

		List<FeatureSample> samples = MeshUtils.sampleFeatures(this.models[this.smoothSteps], this.saliencyMapper, this.colorMapper, new ArrayList<>(), numSamples, new Random());

		//Collect saliency samples
		Collections.sort(samples, (s1, s2) -> -Float.compare(s1.saliency, s2.saliency));
		List<FeatureSample> saliencySamples = new ArrayList<>(samples.subList(0, numSamples / 10));

		float meanEdgeLength = MeshUtils.meanEdgeLength(this.models[0]);

		//Collect color samples
		samples = MeshUtils.sampleFeatures(this.models[this.smoothSteps], this.saliencyMapper, this.colorMapper, new ArrayList<>(), numSamples * 2, new Random());
		Collections.shuffle(samples);
		List<FeatureSample> colorSamples = MeshUtils.selectColorSamples(this.models[0], samples, 6, meanEdgeLength * 2.0f, meanEdgeLength * 1.5f, 0.8f);

		List<FeatureSample> clusterSamples = new ArrayList<>(saliencySamples.size() + colorSamples.size());
		clusterSamples.addAll(saliencySamples);
		clusterSamples.addAll(colorSamples);

		System.out.println("Number of clustered samples: " + clusterSamples.size());

		//Cluster samples
		IsodataClustering clustering = new IsodataClustering(clusterSamples.size() / 20, meanEdgeLength * 2.0f, meanEdgeLength * 10.0f, 10, 100, 200);
		List<Cluster> clusters = clustering.cluster(clusterSamples, new Random());

		//Compute ClusterAngle + Color feature
		this.feature = MeshUtils.computeFeature(clusters, 20);

		System.out.println("Histogram: " + Arrays.toString(this.feature));

		if(this.prevFeature != null) {
			System.out.println("Similarity to previous: " + this.calculateSimilarity(this.feature, this.prevFeature));
		}
		
		float colorStrength = 10.0f;

		if(this.texture != null) GL11.glEnable(GL11.GL_TEXTURE_2D);

		GL11.glEnable(GL11.GL_POLYGON_OFFSET_FILL);
		GL11.glPolygonOffset(100, 1);

		GL11.glMaterialf(GL_FRONT, GL_SHININESS, 120);
		GL11.glBegin(GL_TRIANGLES);
		{
			for (Obj.Face face : this.models[this.smoothSteps].getFaces()) {
				Vector3f[] normals = null;
				if(Math.min(Math.min(face.getNormals()[2], face.getNormals()[1]), face.getNormals()[0]) >= 1 && Math.max(Math.max(face.getNormals()[2], face.getNormals()[1]), face.getNormals()[0]) - 1 < this.models[this.smoothSteps].getNormals().size()) {
					normals = new Vector3f[] {
							this.models[this.smoothSteps].getNormals().get(face.getNormals()[0] - 1),
							this.models[this.smoothSteps].getNormals().get(face.getNormals()[1] - 1),
							this.models[this.smoothSteps].getNormals().get(face.getNormals()[2] - 1)
					};
				} else {
					Vector3f normal = MeshUtils.faceCross(this.models[this.smoothSteps], face).normalise(new Vector3f());
					normals = new Vector3f[] { normal, normal, normal };
				}

				Vector2f[] texCoords = null;
				if(this.texture != null) {
					texCoords = new Vector2f[] {
							this.models[this.smoothSteps].getTextureCoordinates().get(face.getTextureCoords()[0] - 1),
							this.models[this.smoothSteps].getTextureCoordinates().get(face.getTextureCoords()[1] - 1),
							this.models[this.smoothSteps].getTextureCoordinates().get(face.getTextureCoords()[2] - 1)
					};
				}

				Vector3f[] vertices = {
						this.models[this.smoothSteps].getVertices().get(face.getVertices()[0] - 1),
						this.models[this.smoothSteps].getVertices().get(face.getVertices()[1] - 1),
						this.models[this.smoothSteps].getVertices().get(face.getVertices()[2] - 1)
				};

				float[] vertexSaliency = {
						DifferenceOfLaplacian.scoreCurvatures(
								this.scores[3][face.getVertices()[0] - 1],
								this.scores[2][face.getVertices()[0] - 1],
								this.scores[1][face.getVertices()[0] - 1],
								this.scores[0][face.getVertices()[0] - 1]
								),
						DifferenceOfLaplacian.scoreCurvatures(
								this.scores[3][face.getVertices()[1] - 1],
								this.scores[2][face.getVertices()[1] - 1],
								this.scores[1][face.getVertices()[1] - 1],
								this.scores[0][face.getVertices()[1] - 1]
								),
						DifferenceOfLaplacian.scoreCurvatures(
								this.scores[3][face.getVertices()[2] - 1],
								this.scores[2][face.getVertices()[2] - 1],
								this.scores[1][face.getVertices()[2] - 1],
								this.scores[0][face.getVertices()[2] - 1]
								)
				};

				colorByValue(colorMap, Math.abs(vertexSaliency[0]) * colorStrength);
				GL11.glNormal3f(normals[0].getX(), normals[0].getY(), normals[0].getZ());
				if(this.texture != null) GL11.glTexCoord2f(texCoords[0].getX(), 1-texCoords[0].getY());
				GL11.glVertex3f(vertices[0].getX(), vertices[0].getY(), vertices[0].getZ());

				colorByValue(colorMap, Math.abs(vertexSaliency[1]) * colorStrength);
				GL11.glNormal3f(normals[1].getX(), normals[1].getY(), normals[1].getZ());
				if(this.texture != null) GL11.glTexCoord2f(texCoords[1].getX(), 1-texCoords[1].getY());
				GL11.glVertex3f(vertices[1].getX(), vertices[1].getY(), vertices[1].getZ());

				colorByValue(colorMap, Math.abs(vertexSaliency[2]) * colorStrength);
				GL11.glNormal3f(normals[2].getX(), normals[2].getY(), normals[2].getZ());
				if(this.texture != null) GL11.glTexCoord2f(texCoords[2].getX(), 1-texCoords[2].getY());
				GL11.glVertex3f(vertices[2].getX(), vertices[2].getY(), vertices[2].getZ());
			}
		}
		GL11.glEnd();

		GL11.glDisable(GL11.GL_POLYGON_OFFSET_FILL);

		ARBShaderObjects.glUseProgramObjectARB(0);

		GL11.glDisable(GL11.GL_TEXTURE_2D);


		//Weighted normals
		/*GL11.glLineWidth(1.5f);
		GL11.glBegin(GL11.GL_LINES);
		{
			for(int vertexIndex = 0; vertexIndex < this.models[this.smoothSteps].getVertices().size(); vertexIndex++) {
				Vector3f vertex = this.models[this.smoothSteps].getVertices().get(vertexIndex);
				FaceList sharedFaces = this.faceMap[vertexIndex];

				if(sharedFaces != null) {
					Vector3f normal = MeshUtils.weightedNormal(this.models[this.smoothSteps], sharedFaces);

					float len =  0.05f;

					GL11.glColor3f(1, 1, 1);

					GL11.glVertex3f(vertex.x, vertex.y, vertex.z);
					GL11.glVertex3f(vertex.x + normal.x *len, vertex.y + normal.y * len, vertex.z + normal.z * len);
				}
			}
		}	
		GL11.glEnd();*/


		//Render color samples
		{
			GL11.glDisable(GL11.GL_POINT_SMOOTH);

			GL11.glPointSize(9.0f);
			GL11.glBegin(GL11.GL_POINTS);
			{
				for(FeatureSample sample : colorSamples) {
					GL11.glColor3f(0, 0, 0);
					GL11.glVertex3f(sample.position.x, sample.position.y, sample.position.z);
				}
			}	
			GL11.glEnd();

			GL11.glPointSize(7.0f);
			GL11.glBegin(GL11.GL_POINTS);
			{
				for(FeatureSample sample : colorSamples) {
					GL11.glColor3f(sample.color.color.getRed() / 255.0f, sample.color.color.getGreen() / 255.0f, sample.color.color.getBlue() / 255.0f);
					GL11.glVertex3f(sample.position.x, sample.position.y, sample.position.z);
				}
			}	
			GL11.glEnd();
		}


		//Render saliency samples
		{
			GL11.glEnable(GL11.GL_POINT_SMOOTH);

			GL11.glPointSize(9.0f);
			GL11.glBegin(GL11.GL_POINTS);
			{
				for(FeatureSample sample : saliencySamples) {
					GL11.glColor3f(0, 0, 0);
					GL11.glVertex3f(sample.position.x, sample.position.y, sample.position.z);
				}
			}	
			GL11.glEnd();

			GL11.glPointSize(7.0f);
			GL11.glBegin(GL11.GL_POINTS);
			{
				for(FeatureSample sample : saliencySamples) {
					colorByValue(colorMap, Math.abs(sample.saliency) * colorStrength);
					GL11.glVertex3f(sample.position.x, sample.position.y, sample.position.z);
				}
			}	
			GL11.glEnd();
		}


		//Render clusters
		{
			GL11.glDisable(GL11.GL_DEPTH_TEST);

			GL11.glLineWidth(0.5f);
			GL11.glBegin(GL11.GL_LINES);
			{
				for(Cluster cluster : clusters) {
					GL11.glColor3f(1, 1, 1);

					for(FeatureSample sample : cluster.samples) {
						GL11.glVertex3f(cluster.center.x, cluster.center.y, cluster.center.z);
						GL11.glVertex3f(sample.position.x, sample.position.y, sample.position.z);
					}
				}
			}	
			GL11.glEnd();

			GL11.glPointSize(9.0f);
			GL11.glBegin(GL11.GL_POINTS);
			{
				for(Cluster cluster : clusters) {
					GL11.glColor3f(0, 0, 0);
					GL11.glVertex3f(cluster.center.x, cluster.center.y, cluster.center.z);
				}
			}	
			GL11.glEnd();

			GL11.glPointSize(7.0f);
			GL11.glBegin(GL11.GL_POINTS);
			{
				for(Cluster cluster : clusters) {
					GL11.glColor3f(1, 1, 1);
					GL11.glVertex3f(cluster.center.x, cluster.center.y, cluster.center.z);
				}
			}	
			GL11.glEnd();

			GL11.glEnable(GL11.GL_DEPTH_TEST);
		}
	}

	private static void colorByValue(ColorMap colorMap, float value) {
		Color color = colorMap.getColor(Math.max(0, Math.min(1, value)));
		GL11.glColor3f(color.getRed() / 255.0f, color.getGreen() / 255.0f, color.getBlue() / 255.0f);
	}
}
