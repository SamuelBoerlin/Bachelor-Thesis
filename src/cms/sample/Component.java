package cms.sample;

/**
 * This class represents a surface component reconstructed from Hermite Data.
 * The component is structured in such a way that the polygon can be constructed
 * using a triangle fan with the vertex pointed to by the first index being the center
 * point.
 */
public class Component {
	private final int[] packedIndices;
	private final Vec3[] vertices;
	private final Vec3[] normals;
	private final int[] materials;

	private final boolean sharp;
	private final boolean materialTransition;

	/**
	 * Creates a new surface component
	 * @param packedIndices Packed indices, see {@link #getPackedIndices()}
	 * @param vertices Vertices of the component
	 * @param normals Normals of the vertices. Elements corresponding to a reconstructed 2D or 3D sharp feature or a material transition are <code>null</code>
	 * @param materials Materials of the vertices. Elements corresponding to a 3D sharp feature are <code>-1</code>
	 * @param sharp True if this component has a sharp feature, false otherwise
	 * @param materialTransition True if this component has a material transition feature, false otherwise
	 */
	public Component(int[] packedIndices, Vec3[] vertices, Vec3[] normals, int[] materials, boolean sharp, boolean materialTransition) {
		this.packedIndices = packedIndices;
		this.vertices = vertices;
		this.normals = normals;
		this.materials = materials;
		this.sharp = sharp;
		this.materialTransition = materialTransition;
	}

	/**
	 * Returns the packed indices for the component's vertices.
	 * <p>
	 * The indices are ordered counter-clockwise around the surface's normal.
	 * 
	 * <p>
	 * These indices contain additional information (e.g. whether a vertex
	 * is a material transition) so they can't be used as array indices, for
	 * that they need to be unpacked with {@link #unpackIndex(int)} first!
	 */
	public int[] getPackedIndices() {
		return this.packedIndices;
	}

	public static int packIndex(int index, boolean materialTransition, boolean _2DFeature) {
		return (index << 2) | (materialTransition ? 0b1 : 0) | (_2DFeature ? 0b10 : 0);
	}

	/**
	 * Packs the specified new unpacked index with all additional information
	 * from the specified packed index.
	 * @param packedIndex packed index to copy additional information from
	 * @param newUnpackedIndex unpacked index to pack
	 */
	public int repackIndex(int packedIndex, int newUnpackedIndex) {
		return packIndex(newUnpackedIndex, this.isMaterialTransition(packedIndex), this.is2DSharpFeature(packedIndex));
	}

	/**
	 * Returns whether the specified packed index belongs to a material
	 * transition vertex.
	 * @param packedIndex packed index to be checked
	 */
	public boolean isMaterialTransition(int packedIndex) {
		return (packedIndex & 0b1) != 0;
	}

	/**
	 * Returns whether the specified packed index belongs to a 2D sharp
	 * feature vertex.
	 * @param packedIndex packed index to be checked
	 */
	public boolean is2DSharpFeature(int packedIndex) {
		return (packedIndex & 0b10) != 0;
	}

	/**
	 * Unpacks the specified packed index and returns the array index that
	 * can be used for {@link #getVertices()}, {@link #getNormals()} and {@link #getMaterials()}.
	 * @param packedIndex packed index to be unpacked
	 */
	public int unpackIndex(int packedIndex) {
		return packedIndex >> 2;
	}

	/**
	 * Returns the vertices of this component.
	 * A vertex with a <code>null</code> normal is a reconstructed
	 * 2D or 3D sharp feature or a material transition.
	 */
	public Vec3[] getVertices() {
		return this.vertices;
	}

	/**
	 * Returns the normals at the vertices of this component.
	 * If a normal corresponds to reconstructed 2D or 3D sharp feature
	 * or a material transition then it is <code>null</code>.
	 */
	public Vec3[] getNormals() {
		return this.normals;
	}

	/**
	 * Returns the materials at the vertices of this component.
	 * If a material corresponds to a reconstructed 2D or 3D sharp feature then it is <code>0</code>.
	 * @return
	 */
	public int[] getMaterials() {
		return this.materials;
	}

	/**
	 * Returns whether this component has a 3D sharp feature.
	 */
	public boolean hasSharpFeature() {
		return this.sharp;
	}

	/**
	 * Returns whether this component has a material transition feature.
	 */
	public boolean hasMaterialTransitionFeature() {
		return this.materialTransition;
	}
}
