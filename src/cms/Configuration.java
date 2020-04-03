package cms;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

public class Configuration {
	/**
	 * The index mapping of this configuration. Used to compute
	 * the inverse mapping to go from base equivalent configuration + rotation index
	 * to the initial configuration.
	 * Ignored in {@link #equals(Object)}.
	 */
	public RotationMapping mapping = new RotationMapping();

	public boolean[] cornerStates = new boolean[8];

	public static Configuration fromIndex(int index) {
		Configuration configuration = new Configuration();
		for(int i = 0; i < 8; i++) {
			configuration.cornerStates[i] = (index & (1 << i)) != 0;
		}
		return configuration;
	}

	public int index() {
		int index = 0;
		for(int i = 0; i < 8; i++) {
			index |= this.cornerStates[i] ? (1 << i) : 0;
		}
		return index;
	}

	//https://stackoverflow.com/a/33190472
	public Set<Configuration> rotatedStates() {
		Set<Configuration> rotatedConfigurations = new HashSet<>();

		rotatedConfigurations.addAll(rotations4(this, 0));

		rotatedConfigurations.addAll(rotations4(this.rot90(2, 1), 0));

		rotatedConfigurations.addAll(rotations4(this.rot90(1, 1), 2));
		rotatedConfigurations.addAll(rotations4(this.rot90(-1, 1), 2));

		rotatedConfigurations.addAll(rotations4(this.rot90(1, 2), 1));
		rotatedConfigurations.addAll(rotations4(this.rot90(-1, 2), 1));

		return rotatedConfigurations;
	}

	public static List<Configuration> rotations4(Configuration input, int axis) {
		List<Configuration> configurations = new ArrayList<>();

		for(int i = 0; i < 4; i++) {
			configurations.add(input.rot90(i, axis));
		}

		return configurations;
	}

	public Configuration rot90(int num, int axis) {
		boolean cw = num < 0;

		Configuration modified = this.clone();

		num = Math.abs(num);
		for(int i = 0; i < num; i++) {
			if(cw) {
				modified = modified.rot90CW(axis);
			} else {
				modified = modified.rot90CCW(axis);
			}
		}

		return modified;
	}

	public Configuration rot90CCW(int axis) {
		Configuration modified = new Configuration();

		modified.mapping = this.mapping.rot90CCW(axis);

		switch(axis) {
		case 0: //X
			modified.cornerStates[0] = this.cornerStates[4];
			modified.cornerStates[1] = this.cornerStates[5];
			modified.cornerStates[2] = this.cornerStates[1];
			modified.cornerStates[3] = this.cornerStates[0];
			modified.cornerStates[4] = this.cornerStates[7];
			modified.cornerStates[5] = this.cornerStates[6];
			modified.cornerStates[6] = this.cornerStates[2];
			modified.cornerStates[7] = this.cornerStates[3];
			break;
		case 1: //Y
			modified.cornerStates[0] = this.cornerStates[3];
			modified.cornerStates[1] = this.cornerStates[0];
			modified.cornerStates[2] = this.cornerStates[1];
			modified.cornerStates[3] = this.cornerStates[2];
			modified.cornerStates[4] = this.cornerStates[7];
			modified.cornerStates[5] = this.cornerStates[4];
			modified.cornerStates[6] = this.cornerStates[5];
			modified.cornerStates[7] = this.cornerStates[6];
			break;	
		case 2: //Z
			modified.cornerStates[0] = this.cornerStates[1];
			modified.cornerStates[1] = this.cornerStates[5];
			modified.cornerStates[2] = this.cornerStates[6];
			modified.cornerStates[3] = this.cornerStates[2];
			modified.cornerStates[4] = this.cornerStates[0];
			modified.cornerStates[5] = this.cornerStates[4];
			modified.cornerStates[6] = this.cornerStates[7];
			modified.cornerStates[7] = this.cornerStates[3];
			break;
		default:
			throw new IllegalArgumentException();
		}

		return modified;
	}

	public Configuration rot90CW(int axis) {
		//3x90° rotations = -90° rotation
		return this.rot90CCW(axis).rot90CCW(axis).rot90CCW(axis);
	}

	public String bitString() {
		String bitStr = "";
		for(int i = 0; i < 8; i++) {
			bitStr = bitStr + this.bit(i);
		}
		return bitStr;
	}

	private int bit(int i) {
		return this.cornerStates[i] ? 1 : 0;
	}

	public String asciiString() {
		return String.format(
				"   %8$s------------%7$s\n"
						+ "  /|           /| \n"
						+ " / |          / |  \n"
						+ "%5$s------------%6$s  |\n"
						+ "|  |         |  |   \n"
						+ "|  %4$s---------|--%3$s\n"
						+ "| /          | /  \n"
						+ "|/           |/   \n"
						+ "%1$s------------%2$s"
						, this.bit(0), this.bit(1), this.bit(2), this.bit(3), this.bit(4), this.bit(5), this.bit(6), this.bit(7));
	}

	public int numActive() {
		int active = 0;
		for(int i = 0; i < 8; i++) {
			if(this.cornerStates[i]) active++;
		}
		return active;
	}

	@Override
	protected Configuration clone() {
		Configuration clone = new Configuration();
		clone.mapping = this.mapping.clone();
		clone.cornerStates = this.cornerStates.clone();
		return clone;
	}

	@Override
	public int hashCode() {
		return Arrays.hashCode(this.cornerStates);
	}

	@Override
	public boolean equals(Object obj) {
		return obj instanceof Configuration && Arrays.equals(((Configuration)obj).cornerStates, this.cornerStates);
	}
}