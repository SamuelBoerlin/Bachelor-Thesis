package cms;

public class Tables {
	/**
	 * MS case index -> intersection edge indices,
	 * i.e. the edges that contain a surface intersection/normal
	 */
	public static final byte[][] INTERSECTION_EDGE_TABLE = new byte[][] {
		{},
		{3, 0},
		{0, 1},
		{3, 1},
		{1, 2},
		{3, 2, 1, 0},
		{0, 2},
		{3, 2},
		{2, 3},
		{2, 0},
		{2, 3, 0, 1},
		{2, 1},
		{1, 3},
		{1, 0},
		{0, 3},
		{}
	};
	
	public static final byte[][] FACE_TO_EDGES_TABLE = new byte[][] {
		{0, 9, 4, 8},
		{1, 10, 5, 9},
		{2, 11, 6, 10},
		{3, 8, 7, 11},
		{2, 1, 0, 3},
		{4, 5, 6, 7}
	};
}
