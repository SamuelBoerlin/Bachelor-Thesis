package cms.sample;


/**
 * This class represents an arbitrary cell populated with Hermite Data
 * that is used by a polygonization algorithm to reconstruct the
 * surface. Such a cell could for example be a leaf node of a tree structure such as
 * an octree, or a simple cell of a regular grid.
 * All faces and cells must be aligned with the cartesian X/Y/Z coordinate system.
 * <pre>
 * Y+
 * |
 * |   Z+
 * |  /
 * | /
 * |/
 * ----------X+
 * </pre>
 * <p>
 * The values from {@link #getIntersection(int)}, {@link #getNormal(int)} and {@link #getMaterial(int)} comprise
 * the Hermite Data.
 */
public interface HermiteCell {
	/**
	 * <pre>
	 * Faces indices:
	 *    ________________
	 *   /|              /|
	 *  / |     5       / |
	 * /__|____________/  |
	 * |  |       2    |  |
	 * |3 |            |1 |
	 * |  |            |  |
	 * |  |____0_______|__|
	 * | /             |  /
	 * |/       4      | /
	 * o_______________|/
	 * 
	 * Y+
	 * |
	 * |   Z+
	 * |  /
	 * | /
	 * |/
	 * ----------X+
	 * </pre>
	 */
	public static enum Face {
		Z_NEG(0, new Vec3(0, 0, -1), new Vec3(1, 0, 0), new Vec3(0, 1, 0)),
		X_POS(1, new Vec3(1, 0, 0), new Vec3(0, 0, 1), new Vec3(0, 1, 0)),
		Z_POS(2, new Vec3(0, 0, 1), new Vec3(-1, 0, 0), new Vec3(0, 1, 0)),
		X_NEG(3, new Vec3(-1, 0, 0), new Vec3(0, 0, -1), new Vec3(0, 1, 0)),
		Y_NEG(4, new Vec3(0, -1, 0), new Vec3(1, 0, 0), new Vec3(0, 0, -1)),
		Y_POS(5, new Vec3(0, 1, 0), new Vec3(1, 0, 0), new Vec3(0, 0, 1));

		public final int index;
		public final Vec3 normal;
		public final Vec3 basisX, basisY;

		private Face(int index, Vec3 normal, Vec3 baseX, Vec3 baseY) {
			this.index = index;
			this.normal = normal;
			this.basisX = baseX;
			this.basisY = baseY;
		}
	}

	/**
	 * Returns the position of the bottom left corner of face 0 of this cell
	 */
	public Vec3 getPosition();

	/**
	 * Returns the width (X) of this hermite cell
	 */
	public float getWidth();

	/**
	 * Returns the height (Y) of this hermite cell
	 */
	public float getHeight();

	/**
	 * Returns the depth (Z) of this hermite cell
	 */
	public float getDepth();

	/**
	 * Returns the indices of all 2D face cells of the specified face
	 * @param face Face to return the cell indices of
	 */
	public int[] getCells(Face face);

	/**
	 * Returns the 4 indices of all edges of the specified 2D face cell.
	 * <p>
	 * The returned edges must be in this specific order:
	 * <pre>
	 * Face:    X+                     X-
	 * Y+                     Y+                  
	 * ^   _____3_____        ^   _____3_____     
	 * |  |           |       |  |           |    
	 * |  |           |       |  |           |    
	 * |  4           2       |  4           2    
	 * |  |           |       |  |           |    
	 * |  |_____1_____|       |  |_____1_____|    
	 * |                      |                   
	 * o---------------> Z+   o---------------> Z-
	 * 
	 * Face:    Z+                     Z-
	 * Y+                     Y+                  
	 * ^   _____3_____        ^   _____3_____     
	 * |  |           |       |  |           |    
	 * |  |           |       |  |           |    
	 * |  4           2       |  4           2    
	 * |  |           |       |  |           |    
	 * |  |_____1_____|       |  |_____1_____|    
	 * |                      |                   
	 * o---------------> X-   o---------------> X+
	 * 
	 * Face:    Y+                     Y-
	 * Z+                     Z-                  
	 * ^   _____3_____        ^   _____3_____     
	 * |  |           |       |  |           |    
	 * |  |           |       |  |           |    
	 * |  4           2       |  4           2    
	 * |  |           |       |  |           |    
	 * |  |_____1_____|       |  |_____1_____|    
	 * |                      |                   
	 * o---------------> X+   o---------------> X+
	 * </pre>
	 * @param cell Index of the face cell to return the 4 edge indices of
	 */
	public int[] getEdges(int cell);

	/**
	 * Returns the position of 1st corner of the specified 2D face cell.
	 * The 1st corner is specified in {@link #getMaterials(int)}.
	 * @param cell Index of the face cell to return the position of
	 */
	public Vec3 getCellPosition(int cell);

	/**
	 * Returns the width of the specified 2D face cell.
	 * The width is the distance between the 2nd and 1st corner as specified in {@link #getMaterials(int)}.
	 * @param cell Index of the face cell to return the width of
	 */
	public float getCellWidth(int cell);

	/**
	 * Returns the height of the specified 2D face cell.
	 * The height is the distance between the 4th and 1st corner as specified in {@link #getMaterials(int)}.
	 * @param cell Index of the face cell to return the height of
	 */
	public float getCellHeight(int cell);

	/**
	 * Returns the materials at the four corners of the specified 2D face cell.
	 * <p>
	 * The corners must be in this specific order:
	 * <pre>
	 * Face:    X+                     X-
	 * Y+                     Y+                  
	 * ^  4-----------3       ^  4-----------3    
	 * |  |           |       |  |           |    
	 * |  |           |       |  |           |    
	 * |  |           |       |  |           |    
	 * |  |           |       |  |           |    
	 * |  1-----------2       |  1-----------2    
	 * |                      |                   
	 * o---------------> Z+   o---------------> Z-
	 * 
	 * Face:    Z+                     Z-
	 * Y+                     Y+                  
	 * ^  4-----------3       ^  4-----------3    
	 * |  |           |       |  |           |    
	 * |  |           |       |  |           |    
	 * |  |           |       |  |           |    
	 * |  |           |       |  |           |    
	 * |  1-----------2       |  1-----------2    
	 * |                      |                   
	 * o---------------> X-   o---------------> X+
	 * 
	 * Face:    Y+                     Y-
	 * Z+                     Z-                  
	 * ^  4-----------3       ^  4-----------3    
	 * |  |           |       |  |           |    
	 * |  |           |       |  |           |    
	 * |  |           |       |  |           |    
	 * |  |           |       |  |           |    
	 * |  1-----------2       |  1-----------2    
	 * |                      |                   
	 * o---------------> X+   o---------------> X+
	 * </pre>
	 * @param cell Index of the face cell to return the 4 materials of
	 */
	public int[] getMaterials(int cell);

	/**
	 * Returns the indices of all neighboring, i.e. touching, edges of the specified edge of a 2D face cell
	 * @param cell Index of the face cell the edge belongs to
	 * @param edge Index of the edge to return the neighboring edge indices of
	 */
	public int[] getNeighboringEdges(int cell, int edge);

	/**
	 * Returns the surface intersection point on the specified edge of a 2D face cell.
	 * <p>
	 * The returned intersection point must follow a specific direction:
	 * <pre>
	 *  <-----3------
	 * |             ^
	 * |             |
	 * 4             2
	 * |             |
	 * v             |
	 *  ------1----->
	 * 
	 * Where 1, 2, 3 and 4 is the ordering of the edges as specified in {@link #getEdges(int)}
	 * </pre>
	 * @param cell Index of the face cell the edge belongs to
	 * @param edge Index of the 2D face cell edge to return the intersection point of
	 */
	public float getIntersection(int cell, int edge);

	/**
	 * Returns whether the specified edge of a 2D face cell has a surface intersection and normal.
	 * See also {@link #getIntersection(int, int)}.
	 * @param cell Index of the face cell the edge belongs to
	 * @param edge Index of the 2D face cell edge to return the intersection point of
	 */
	public boolean hasIntersection(int cell, int edge);

	/**
	 * Returns the surface normal on the specified edge of a 2D face cell
	 * @param cell Index of the face cell the edge belongs to
	 * @param edge Index of the 2D face cell edge to return the surface normal of
	 */
	public Vec3 getNormal(int cell, int edge);
}
