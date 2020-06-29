package cms.sample;

import java.util.function.Function;
import java.util.function.Predicate;

/**
 * These are all methods that would be off-loaded to a GPU for parallel processing using algorithms like the prefix scan: http://developer.download.nvidia.com/compute/cuda/1_1/Website/projects/scan/doc/scan.pdf
 */
public class GPU {
	public static int[] inclusiveScan(Function<Integer, Integer> data, int length) {
		int[] scan = new int[length];
		int accumulator = 0;
		for(int i = 0; i < length; i++) {
			accumulator += data.apply(i);
			scan[i] = accumulator;
		}
		return scan;
	}

	public static int[] exclusiveScan(Function<Integer, Integer> data, int length) {
		int[] scan = new int[length];
		int accumulator = 0;
		for(int i = 0; i < length; i++) {
			scan[i] = accumulator;
			accumulator += data.apply(i);
		}
		return scan;
	}

	public static <T> T[] compact(Function<Integer, T> data, Predicate<T> predicate, int length, int[] exclusiveBoolScan) {
		@SuppressWarnings("unchecked")
		T[] compacted = (T[]) new Object[exclusiveBoolScan[exclusiveBoolScan.length - 1]];
		for(int i = 0; i < length; i++) {
			T obj = data.apply(i);
			if(predicate.test(obj)) {
				compacted[exclusiveBoolScan[i]] = obj;
			}
		}
		return compacted;
	}

	public static <T> int[] compactIndices(Function<Integer, T> data, Predicate<T> predicate, int length, int[] exclusiveBoolScan) {
		int[] indices = new int[exclusiveBoolScan.length - 1];
		for(int i = 0; i < length; i++) {
			T obj = data.apply(i);
			if(predicate.test(obj)) {
				indices[exclusiveBoolScan[i]] = i;
			}
		}
		return indices;
	}

	public static int[] occupancy(Function<Integer, Integer> data, int[] inclusiveDataScan) {
		int[] occupancy = new int[inclusiveDataScan[inclusiveDataScan.length - 1]];
		for(int i = 0; i < inclusiveDataScan.length; i++) {
			int value = data.apply(i);
			if(value > 0) {
				int scan = inclusiveDataScan[i];
				int index = scan - value; //since the scan is inclusive we need to remove its own count to get the start index
				for(int j = 0; j < value; j++) {
					occupancy[index + j] = i;
				}
			}
		}
		return occupancy;
	}
}
