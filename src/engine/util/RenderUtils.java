package engine.util;

import org.lwjgl.opengl.Display;
import org.lwjgl.opengl.GL11;
import org.lwjgl.util.glu.GLU;

public class RenderUtils {
	public static void renderSimpleRect(float x, float y, float x2, float y2, int color, int z) {
		Vertex2D v1 = new Vertex2D(x, y, color);
		Vertex2D v2 = new Vertex2D(x, y2, color);
		Vertex2D v3 = new Vertex2D(x2, y2, color);
		Vertex2D v4 = new Vertex2D(x2, y, color);
		renderRect(v1, v2, v3, v4, z);
	}
	
	public static void renderRect(Vertex2D v1, Vertex2D v2, Vertex2D v3, Vertex2D v4, int z) {
		float[] c1 = v1.getRGBA();
		float[] c2 = v2.getRGBA();
		float[] c3 = v3.getRGBA();
		float[] c4 = v4.getRGBA();
		
		GL11.glBegin(GL11.GL_TRIANGLES);
		GL11.glColor4f(c1[0], c1[1], c1[2], c1[3]);
		GL11.glVertex3d(v1.x, v1.y, z);
		GL11.glColor4f(c2[0], c2[1], c2[2], c2[3]);
		GL11.glVertex3d(v2.x, v2.y, z);
		GL11.glColor4f(c3[0], c3[1], c3[2], c3[3]);
		GL11.glVertex3d(v3.x, v3.y, z);
		GL11.glVertex3d(v3.x, v3.y, z);
		GL11.glColor4f(c4[0], c4[1], c4[2], c4[3]);
		GL11.glVertex3d(v4.x, v4.y, z);
		GL11.glColor4f(c1[0], c1[1], c1[2], c1[3]);
		GL11.glVertex3d(v1.x, v1.y, z);
		GL11.glEnd();
	}
	
	public static void renderArrow() {
		GL11.glBegin(GL11.GL_TRIANGLE_FAN);
		GL11.glVertex3d(0, 1, 0);
		GL11.glVertex3d(0, 0.9D, 0.05D);
		GL11.glVertex3d(0.05D, 0.9D, 0);
		GL11.glVertex3d(0, 0.9D, -0.05D);
		GL11.glVertex3d(-0.05D, 0.9D, 0);
		GL11.glVertex3d(0, 0.9D, 0.05D);
		GL11.glEnd();

		GL11.glBegin(GL11.GL_TRIANGLE_FAN);
		GL11.glVertex3d(0, 0.9D, 0);
		GL11.glVertex3d(0.05D, 0.9D, 0);
		GL11.glVertex3d(0, 0.9D, 0.05D);
		GL11.glVertex3d(-0.05D, 0.9D, 0);
		GL11.glVertex3d(0, 0.9D, -0.05D);
		GL11.glVertex3d(0.05D, 0.9D, 0);
		GL11.glEnd();

		GL11.glBegin(GL11.GL_LINES);
		GL11.glVertex3d(0, 0, 0);
		GL11.glVertex3d(0, 1, 0);
		GL11.glEnd();
	}

	public static void renderCube() {
		GL11.glBegin(GL11.GL_LINE_STRIP);
		GL11.glVertex3d(0, 0, 0);
		GL11.glVertex3d(1, 0, 0);
		GL11.glVertex3d(1, 1, 0);
		GL11.glVertex3d(0, 1, 0);
		GL11.glVertex3d(0, 0, 0);
		GL11.glEnd();

		GL11.glBegin(GL11.GL_LINE_STRIP);
		GL11.glVertex3d(1, 0, 0);
		GL11.glVertex3d(1, 0, 1);
		GL11.glVertex3d(1, 1, 1);
		GL11.glVertex3d(1, 1, 0);
		GL11.glVertex3d(1, 0, 0);
		GL11.glEnd();

		GL11.glBegin(GL11.GL_LINE_STRIP);
		GL11.glVertex3d(1, 0, 1);
		GL11.glVertex3d(0, 0, 1);
		GL11.glVertex3d(0, 1, 1);
		GL11.glVertex3d(1, 1, 1);
		GL11.glVertex3d(1, 0, 1);
		GL11.glEnd();

		GL11.glBegin(GL11.GL_LINE_STRIP);
		GL11.glVertex3d(0, 0, 1);
		GL11.glVertex3d(0, 0, 0);
		GL11.glVertex3d(0, 1, 0);
		GL11.glVertex3d(0, 1, 1);
		GL11.glVertex3d(0, 0, 1);
		GL11.glEnd();
	}

	public static void createCubeFullQuads(double x, double y, double z, double w, double h, double d) {
		GL11.glNormal3f(0, -1, 0);
		GL11.glVertex3d(x, y, z);
		GL11.glNormal3f(0, -1, 0);
		GL11.glVertex3d(x, y, z + d);
		GL11.glNormal3f(0, -1, 0);
		GL11.glVertex3d(x + w, y, z + d);
		GL11.glNormal3f(0, -1, 0);
		GL11.glVertex3d(x + w, y, z);

		GL11.glNormal3f(0, 1, 0);
		GL11.glVertex3d(x, y + h, z + d);
		GL11.glNormal3f(0, 1, 0);
		GL11.glVertex3d(x, y + h, z);
		GL11.glNormal3f(0, 1, 0);
		GL11.glVertex3d(x + w, y + h, z);
		GL11.glNormal3f(0, 1, 0);
		GL11.glVertex3d(x + w, y + h, z + d);

		GL11.glNormal3f(0, 0, -1);
		GL11.glVertex3d(x, y + h, z);
		GL11.glNormal3f(0, 0, -1);
		GL11.glVertex3d(x, y, z);
		GL11.glNormal3f(0, 0, -1);
		GL11.glVertex3d(x + w, y, z);
		GL11.glNormal3f(0, 0, -1);
		GL11.glVertex3d(x + w, y + h, z);

		GL11.glNormal3f(0, 0, 1);
		GL11.glVertex3d(x + w, y + h, z + d);
		GL11.glNormal3f(0, 0, 1);
		GL11.glVertex3d(x + w, y, z + d);
		GL11.glNormal3f(0, 0, 1);
		GL11.glVertex3d(x, y, z + d);
		GL11.glNormal3f(0, 0, 1);
		GL11.glVertex3d(x, y + h, z + d);

		GL11.glNormal3f(-1, 0, 0);
		GL11.glVertex3d(x, y + h, z + d);
		GL11.glNormal3f(-1, 0, 0);
		GL11.glVertex3d(x, y, z + d);
		GL11.glNormal3f(-1, 0, 0);
		GL11.glVertex3d(x, y, z);
		GL11.glNormal3f(-1, 0, 0);
		GL11.glVertex3d(x, y + h, z);

		GL11.glNormal3f(1, 0, 0);
		GL11.glVertex3d(x + w, y + h, z);
		GL11.glNormal3f(1, 0, 0);
		GL11.glVertex3d(x + w, y, z);
		GL11.glNormal3f(1, 0, 0);
		GL11.glVertex3d(x + w, y, z + d);
		GL11.glNormal3f(1, 0, 0);
		GL11.glVertex3d(x + w, y + h, z + d);
	}

	public static void renderCubeFull() {
		GL11.glBegin(GL11.GL_QUADS);

		createCubeFullQuads(0, 0, 0, 1, 1, 1);

		GL11.glEnd();
	}

	public static void setupPerspective(int x1, int y1, int x2, int y2, float near, float far) {
		//Set up perspective
		GL11.glMatrixMode(GL11.GL_PROJECTION);
		GL11.glLoadIdentity();

		GLU.gluPerspective(75, (float)Display.getWidth() / (float)Display.getHeight(), near, far);

		//Set up modelview
		GL11.glMatrixMode(GL11.GL_MODELVIEW);
		GL11.glLoadIdentity();

		//Alpha blending
		GL11.glBlendFunc(GL11.GL_SRC_ALPHA, GL11.GL_ONE_MINUS_SRC_ALPHA);
		GL11.glEnable(GL11.GL_BLEND);

		//Backface culling
		GL11.glEnable(GL11.GL_CULL_FACE);
		GL11.glCullFace(GL11.GL_BACK);

		//Enable depth test
		GL11.glEnable(GL11.GL_DEPTH_TEST);
		GL11.glDepthFunc(GL11.GL_LEQUAL);

		//Enable depth mask
		GL11.glDepthMask(true);

		//Smooth shading for vertex color interpolation
		GL11.glShadeModel(GL11.GL_SMOOTH);
	}
}
