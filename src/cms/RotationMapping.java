package cms;
import java.util.Arrays;

public class RotationMapping {
	public int[] cornerIndices = new int[8];
	public int[] edgeIndices = new int[12];
	public int[] faceIndices = new int[6];

	/**
	 * Which edges appear as flipped when looking at the rotated cube in +Z direction.
	 * Edges initially always face in the positive direction of their axis.
	 * This is required to be able to properly position edge intersection points in the base configuration since
	 * the edges are implicitly directed and the intersection point is simply stored as an "undirected" float.
	 */
	public boolean[] edgeFlips = new boolean[12];

	RotationMapping() {
		for(int i = 0; i < 8; i++) {
			this.cornerIndices[i] = i;
		}
		for(int i = 0; i < 12; i++) {
			this.edgeIndices[i] = i;
		}
		for(int i = 0; i < 6; i++) {
			this.faceIndices[i] = i;
		}
	}

	public RotationMapping rot90CCW(int axis) {
		RotationMapping modified = this.clone();

		switch(axis) {
		case 0: //X
			modified.cornerIndices[0] = this.cornerIndices[4];
			modified.cornerIndices[1] = this.cornerIndices[5];
			modified.cornerIndices[2] = this.cornerIndices[1];
			modified.cornerIndices[3] = this.cornerIndices[0];
			modified.cornerIndices[4] = this.cornerIndices[7];
			modified.cornerIndices[5] = this.cornerIndices[6];
			modified.cornerIndices[6] = this.cornerIndices[2];
			modified.cornerIndices[7] = this.cornerIndices[3];

			modified.edgeIndices[0] = this.edgeIndices[4];
			modified.edgeIndices[1] = this.edgeIndices[9];
			modified.edgeIndices[2] = this.edgeIndices[0];
			modified.edgeIndices[3] = this.edgeIndices[8];
			modified.edgeIndices[4] = this.edgeIndices[6];
			modified.edgeIndices[5] = this.edgeIndices[10];
			modified.edgeIndices[6] = this.edgeIndices[2];
			modified.edgeIndices[7] = this.edgeIndices[11];
			modified.edgeIndices[8] = this.edgeIndices[7];
			modified.edgeIndices[9] = this.edgeIndices[5];
			modified.edgeIndices[10] = this.edgeIndices[1];
			modified.edgeIndices[11] = this.edgeIndices[3];

			modified.faceIndices[0] = this.faceIndices[5];
			modified.faceIndices[1] = this.faceIndices[1];
			modified.faceIndices[2] = this.faceIndices[4];
			modified.faceIndices[3] = this.faceIndices[3];
			modified.faceIndices[4] = this.faceIndices[0];
			modified.faceIndices[5] = this.faceIndices[2];

			modified.edgeFlips[0] = this.edgeFlips[4];
			modified.edgeFlips[1] = !this.edgeFlips[9]; //flip
			modified.edgeFlips[2] = this.edgeFlips[0];
			modified.edgeFlips[3] = !this.edgeFlips[8]; //flip
			modified.edgeFlips[4] = this.edgeFlips[6];
			modified.edgeFlips[5] = !this.edgeFlips[10]; //flip
			modified.edgeFlips[6] = this.edgeFlips[2];
			modified.edgeFlips[7] = !this.edgeFlips[11]; //flip
			modified.edgeFlips[8] = this.edgeFlips[7];
			modified.edgeFlips[9] = this.edgeFlips[5];
			modified.edgeFlips[10] = this.edgeFlips[1];
			modified.edgeFlips[11] = this.edgeFlips[3];
			break;
		case 1: //Y
			modified.cornerIndices[0] = this.cornerIndices[3];
			modified.cornerIndices[1] = this.cornerIndices[0];
			modified.cornerIndices[2] = this.cornerIndices[1];
			modified.cornerIndices[3] = this.cornerIndices[2];
			modified.cornerIndices[4] = this.cornerIndices[7];
			modified.cornerIndices[5] = this.cornerIndices[4];
			modified.cornerIndices[6] = this.cornerIndices[5];
			modified.cornerIndices[7] = this.cornerIndices[6];

			modified.edgeIndices[0] = this.edgeIndices[3];
			modified.edgeIndices[1] = this.edgeIndices[0];
			modified.edgeIndices[2] = this.edgeIndices[1];
			modified.edgeIndices[3] = this.edgeIndices[2];
			modified.edgeIndices[4] = this.edgeIndices[7];
			modified.edgeIndices[5] = this.edgeIndices[4];
			modified.edgeIndices[6] = this.edgeIndices[5];
			modified.edgeIndices[7] = this.edgeIndices[6];
			modified.edgeIndices[8] = this.edgeIndices[11];
			modified.edgeIndices[9] = this.edgeIndices[8];
			modified.edgeIndices[10] = this.edgeIndices[9];
			modified.edgeIndices[11] = this.edgeIndices[10];

			modified.faceIndices[0] = this.faceIndices[3];
			modified.faceIndices[1] = this.faceIndices[0];
			modified.faceIndices[2] = this.faceIndices[1];
			modified.faceIndices[3] = this.faceIndices[2];
			modified.faceIndices[4] = this.faceIndices[4];
			modified.faceIndices[5] = this.faceIndices[5];

			modified.edgeFlips[0] = !this.edgeFlips[3]; //flip
			modified.edgeFlips[1] = this.edgeFlips[0];
			modified.edgeFlips[2] = !this.edgeFlips[1]; //flip
			modified.edgeFlips[3] = this.edgeFlips[2];
			modified.edgeFlips[4] = !this.edgeFlips[7]; //flip
			modified.edgeFlips[5] = this.edgeFlips[4];
			modified.edgeFlips[6] = !this.edgeFlips[5]; //flip
			modified.edgeFlips[7] = this.edgeFlips[6];
			modified.edgeFlips[8] = this.edgeFlips[11];
			modified.edgeFlips[9] = this.edgeFlips[8];
			modified.edgeFlips[10] = this.edgeFlips[9];
			modified.edgeFlips[11] = this.edgeFlips[10];
			break;	
		case 2: //Z
			modified.cornerIndices[0] = this.cornerIndices[1];
			modified.cornerIndices[1] = this.cornerIndices[5];
			modified.cornerIndices[2] = this.cornerIndices[6];
			modified.cornerIndices[3] = this.cornerIndices[2];
			modified.cornerIndices[4] = this.cornerIndices[0];
			modified.cornerIndices[5] = this.cornerIndices[4];
			modified.cornerIndices[6] = this.cornerIndices[7];
			modified.cornerIndices[7] = this.cornerIndices[3];

			modified.edgeIndices[0] = this.edgeIndices[9];
			modified.edgeIndices[1] = this.edgeIndices[5];
			modified.edgeIndices[2] = this.edgeIndices[10];
			modified.edgeIndices[3] = this.edgeIndices[1];
			modified.edgeIndices[4] = this.edgeIndices[8];
			modified.edgeIndices[5] = this.edgeIndices[7];
			modified.edgeIndices[6] = this.edgeIndices[11];
			modified.edgeIndices[7] = this.edgeIndices[3];
			modified.edgeIndices[8] = this.edgeIndices[0];
			modified.edgeIndices[9] = this.edgeIndices[4];
			modified.edgeIndices[10] = this.edgeIndices[6];
			modified.edgeIndices[11] = this.edgeIndices[2];

			modified.faceIndices[0] = this.faceIndices[0];
			modified.faceIndices[1] = this.faceIndices[5];
			modified.faceIndices[2] = this.faceIndices[2];
			modified.faceIndices[3] = this.faceIndices[4];
			modified.faceIndices[4] = this.faceIndices[1];
			modified.faceIndices[5] = this.faceIndices[3];

			modified.edgeFlips[0] = this.edgeFlips[9];
			modified.edgeFlips[1] = this.edgeFlips[5];
			modified.edgeFlips[2] = this.edgeFlips[10];
			modified.edgeFlips[3] = this.edgeFlips[1];
			modified.edgeFlips[4] = this.edgeFlips[8];
			modified.edgeFlips[5] = this.edgeFlips[7];
			modified.edgeFlips[6] = this.edgeFlips[11];
			modified.edgeFlips[7] = this.edgeFlips[3];
			modified.edgeFlips[8] = !this.edgeFlips[0]; //flip
			modified.edgeFlips[9] = !this.edgeFlips[4]; //flip
			modified.edgeFlips[10] = !this.edgeFlips[6]; //flip
			modified.edgeFlips[11] = !this.edgeFlips[2]; //flip
			break;
		default:
			throw new IllegalArgumentException();
		}

		return modified;
	}

	public RotationMapping invertIndices() {
		RotationMapping inverse = this.clone();
		for(int i = 0; i < 8; i++) {
			inverse.cornerIndices[this.cornerIndices[i]] = i;
		}
		for(int i = 0; i < 12; i++) {
			inverse.edgeIndices[this.edgeIndices[i]] = i;
		}
		for(int i = 0; i < 6; i++) {
			inverse.faceIndices[this.faceIndices[i]] = i;
		}
		return inverse;
	}

	public void assertUnrotated() {
		for(int i = 0; i < 8; i++) {
			if(i != this.cornerIndices[i]) {
				throw new RuntimeException("Failed unrotated assertion");
			}
		}
		for(int i = 0; i < 12; i++) {
			if(i != this.edgeIndices[i]) {
				throw new RuntimeException("Failed unrotated assertion");
			}
		}
		for(int i = 0; i < 6; i++) {
			if(i != this.faceIndices[i]) {
				throw new RuntimeException("Failed unrotated assertion");
			}
		}
		for(int i = 0; i < 12; i++) {
			if(this.edgeFlips[i]) {
				throw new RuntimeException("Failed unrotated assertion");
			}
		}
	}

	@Override
	protected RotationMapping clone() {
		RotationMapping mappings = new RotationMapping();
		mappings.cornerIndices = this.cornerIndices.clone();
		mappings.edgeIndices = this.edgeIndices.clone();
		mappings.faceIndices = this.faceIndices.clone();
		mappings.edgeFlips = this.edgeFlips.clone();
		return mappings;
	}

	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result + Arrays.hashCode(cornerIndices);
		result = prime * result + Arrays.hashCode(edgeIndices);
		result = prime * result + Arrays.hashCode(faceIndices);
		result = prime * result + Arrays.hashCode(edgeFlips);
		return result;
	}

	@Override
	public boolean equals(Object obj) {
		if (this == obj)
			return true;
		if (obj == null)
			return false;
		if (getClass() != obj.getClass())
			return false;
		RotationMapping other = (RotationMapping) obj;
		if (!Arrays.equals(cornerIndices, other.cornerIndices))
			return false;
		if (!Arrays.equals(edgeIndices, other.edgeIndices))
			return false;
		if (!Arrays.equals(faceIndices, other.faceIndices))
			return false;
		if (!Arrays.equals(edgeFlips, other.edgeFlips))
			return false;
		return true;
	}
}