package cms.sample;


public interface QEFSolver {
	/**
	 * Finds the minimum quadratic error solution for the given left hand side and right hand side of a linear system of equations
	 * @param cell
	 * @param lhs
	 * @param rhs
	 * @param is3DSharpCornerFeature
	 * @param mean
	 * @return
	 */
	public Vec3 solve(HermiteCell cell, float[][] lhs, float[][] rhs, boolean is3DSharpCornerFeature, Vec3 mean);
}
