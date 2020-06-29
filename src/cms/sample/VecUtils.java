package cms.sample;


import cms.sample.Vec3;

public class VecUtils {
	public static double projectedNormalizedDot(Vec3 dir1, Vec3 dir2, Vec3 normal) {
		return projectOnPlane(dir1, normal).normalized().dot(projectOnPlane(dir2, normal).normalized());
	}

	public static Vec3 projectOnPlane(Vec3 dir, Vec3 normal) {
		return dir.sub(normal.mul(dir.dot(normal)));
	}

	public static Vec3 intersectPlanes(Vec3 p1, Vec3 n1, Vec3 p2, Vec3 n2, Vec3 p3, Vec3 n3) {
		double d1 = -n1.dot(p1);
		double d2 = -n2.dot(p2);
		double d3 = -n3.dot(p3);
		return n2.cross(n3).mul(-d1).add(n3.cross(n1).mul(-d2)).add(n1.cross(n2).mul(-d3)).mul(1.0D / n1.dot(n2.cross(n3)));
	}
}
