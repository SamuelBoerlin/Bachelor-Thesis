package cms;

public class BaseRotationPair {
	public final int base;
	public final int rotation;

	public BaseRotationPair(int base, int rotation) {
		this.base = base;
		this.rotation = rotation;
	}

	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result + base;
		result = prime * result + rotation;
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
		BaseRotationPair other = (BaseRotationPair) obj;
		if (base != other.base)
			return false;
		if (rotation != other.rotation)
			return false;
		return true;
	}
}