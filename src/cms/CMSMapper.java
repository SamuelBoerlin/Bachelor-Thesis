package cms;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.Collections;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

public class CMSMapper {
	public static class CMSResult {
		public final List<CellComponents> combinations;
		public final List<Integer> ambiguousFaces;

		public CMSResult(List<CellComponents> combinations, List<Integer> ambiguousFaces) {
			this.combinations = Collections.unmodifiableList(combinations);
			this.ambiguousFaces = Collections.unmodifiableList(ambiguousFaces);
		}
	}

	public static class CellComponents {
		public final List<Integer> components;
		public final int ambiguityIndex;

		public CellComponents(List<Integer> components, int ambiguityIndex) {
			this.components = Collections.unmodifiableList(components);
			this.ambiguityIndex = ambiguityIndex;
		}
	}

	public static class Component {
		public final List<Integer> edges;

		private final List<Integer> hashList;
		private final int hash;

		public Component(Collection<Integer> edges) {
			this.edges = Collections.unmodifiableList(new ArrayList<>(edges));

			final int numEdges = this.edges.size();

			//Reorder loop to always start at the min edge to make equality independent of loop start
			int minEdge = 12;
			int minIndex = -1;
			for(int i = 0; i < numEdges; i++) {
				int edge = this.edges.get(i);
				if(edge < minEdge) {
					minEdge = edge;
					minIndex = i;
				}
			}
			this.hashList = new ArrayList<>();
			for(int i = 0; i < numEdges; i++) {
				this.hashList.add(this.edges.get((minIndex + i) % numEdges));
			}
			this.hash = this.hashList.hashCode();
		}

		public String asciiString() {
			int[] edgeOrder = new int[12];
			int order = 1;
			for(int edge : this.edges) {
				edgeOrder[edge] = order;
				order++;
			}
			return String.format(
					"    o------%7$s----o \n" + 
							"   /|           /| \n" + 
							"  %8$s|          %6$s| \n" + 
							" /  %12$s        /  %11$s\n" + 
							"o------%5$s----o   | \n" + 
							"|   |        |   | \n" + 
							"|   o-----%3$s-|---o \n" + 
							"%9$s /         %10$s /  \n" + 
							"| %4$s         | %2$s  \n" + 
							"|/           |/    \n" + 
							"o------%1$s----o",
							(edgeOrder[0] > 0 ? this.pad(edgeOrder[0], "-") : "--"),
							(edgeOrder[1] > 0 ? this.pad(edgeOrder[1], " ") : "/ "),
							(edgeOrder[2] > 0 ? this.pad(edgeOrder[2], "-") : "--"),
							(edgeOrder[3] > 0 ? this.pad(edgeOrder[3], " ") : "/ "),
							(edgeOrder[4] > 0 ? this.pad(edgeOrder[4], "-") : "--"),
							(edgeOrder[5] > 0 ? this.pad(edgeOrder[5], " ") : "/ "),
							(edgeOrder[6] > 0 ? this.pad(edgeOrder[6], "-") : "--"),
							(edgeOrder[7] > 0 ? this.pad(edgeOrder[7], " ") : "/ "),
							(edgeOrder[8] > 0 ? this.pad(edgeOrder[8], " ") : "| "),
							(edgeOrder[9] > 0 ? this.pad(edgeOrder[9], " ") : "| "),
							(edgeOrder[10] > 0 ? this.pad(edgeOrder[10], " ") : "| "),
							(edgeOrder[11] > 0 ? this.pad(edgeOrder[11], " ") : "| ")
					);
		}

		private String pad(int nr, String padding) {
			return nr >= 10 ? String.valueOf(nr) : nr + padding;
		}

		@Override
		public boolean equals(Object obj) {
			return obj instanceof Component && ((Component)obj).hashList.equals(this.hashList);
		}

		@Override
		public int hashCode() {
			return this.hash;
		}
	}

	private static class Segment {
		public final int e1, e2;

		private Segment(int e1, int e2) {
			this.e1 = e1;
			this.e2 = e2;
		}
	}

	public static CMSResult cms(Configuration configuration, Map<Integer, Component> indexToComponentMap, Map<Component, Integer> componentToIndexMap) {
		List<List<List<Segment>>> cellSegments = new ArrayList<>();

		List<Integer> ambiguousFaces = new ArrayList<>();

		for(int i = 0; i < 6; i++) {
			List<List<Segment>> faceSegments = new ArrayList<>();

			ms(faceSegments, i, configuration);

			if(faceSegments.size() > 2) {
				throw new IllegalStateException("Marching squares has returned more than 2 potential configurations");
			}

			if(faceSegments.size() == 2) {
				ambiguousFaces.add(i);
			}

			cellSegments.add(faceSegments);
		}

		//Total number of possible combinations of face ambiguities
		int combinations = 1 << ambiguousFaces.size();

		List<CellComponents> cellComponents = new ArrayList<>();

		//Go through all possible combinations of face ambiguities
		for(int ambiguityIndex = 0; ambiguityIndex < combinations; ambiguityIndex++) {
			List<Segment> segments = new ArrayList<>();
			int choiceBit = 0;

			//Get segments for the given ambiguity configuration
			for(int j = 0; j < 6; j++) {
				List<List<Segment>> faceSegments = cellSegments.get(j);

				if(faceSegments.size() == 2) {
					int choice = ((ambiguityIndex >> choiceBit) & 0b1) != 0 ? 1 : 0;

					segments.addAll(faceSegments.get(choice));

					choiceBit++;
				} else if(faceSegments.size() == 1) {
					segments.addAll(faceSegments.get(0));
				}
			}

			List<Component> components = traceComponents(segments);

			//Enumerate components
			List<Integer> componentIndices = new ArrayList<>();
			for(Component component : components) {
				if(componentToIndexMap.containsKey(component)) {
					componentIndices.add(componentToIndexMap.get(component));
				} else {
					int newIndex = componentToIndexMap.size();
					componentIndices.add(newIndex);
					componentToIndexMap.put(component, newIndex);
					indexToComponentMap.put(newIndex, component);
				}
			}

			cellComponents.add(new CellComponents(componentIndices, ambiguityIndex));
		}

		CMSResult result = new CMSResult(cellComponents, ambiguousFaces);

		if((1 << result.ambiguousFaces.size()) != result.combinations.size()) {
			throw new IllegalStateException("CMSResult component combinations count and actual combinations count do not match");
		}

		return result;
	}

	private static List<Component> traceComponents(List<Segment> segments) {
		List<Component> components = new ArrayList<>();

		while(!segments.isEmpty()) {
			List<Integer> loop = new ArrayList<>();

			Segment current = segments.get(0);
			segments.remove(0);

			loop.add(current.e1);
			loop.add(current.e2);

			boolean foundNext;
			do {
				foundNext = false;

				for(Segment next : segments) {
					if(current.e2 == next.e1) {
						loop.add(next.e2);

						segments.remove(next);
						current = next;

						foundNext = true;
						break;
					}
				}
			} while(foundNext);

			if(loop.get(0) != loop.get(loop.size() - 1)) {
				throw new IllegalStateException("Component loop is not closed");
			}

			//Remove last edge because it's the same as the first
			loop.remove(loop.size() - 1);

			if(loop.size() < 3) {
				throw new IllegalStateException("Component loop has less than 3 vertices: " + loop.size());
			}

			Set<Integer> unique = new HashSet<>(loop);
			if(unique.size() != loop.size()) {
				throw new IllegalStateException("Component loop has duplicate edge");
			}

			components.add(new Component(loop));
		}

		return components;
	}

	public static void mapEdges(int[] edges, int face) {
		switch(face) {
		case 0:
			edges[0]  = 0;
			edges[1]  = 9;
			edges[2]  = 4;
			edges[3]  = 8;
			break;
		case 1:
			edges[0]   = 1;
			edges[1]   = 10;
			edges[2]   = 5;
			edges[3]   = 9;
			break;
		case 2:
			edges[0]   = 2;
			edges[1]   = 11;
			edges[2]   = 6;
			edges[3]   = 10;
			break;
		case 3:
			edges[0]   = 3;
			edges[1]   = 8;
			edges[2]   = 7;
			edges[3]   = 11;
			break;
		case 4: 
			edges[0]   = 2;
			edges[1]   = 1;
			edges[2]   = 0;
			edges[3]   = 3;
			break;
		case 5:
			edges[0]   = 4;
			edges[1]   = 5;
			edges[2]   = 6;
			edges[3]   = 7;
			break;
		default:
			throw new IllegalArgumentException();
		}
	}
	
	private static void ms(List<List<Segment>> faceSegments, int face, Configuration configuration) {
		boolean[] corners = new boolean[4];
		int[] edges = new int[4];

		mapEdges(edges, face);
		
		switch(face) {
		case 0:
			corners[0] = configuration.cornerStates[0];
			corners[1] = configuration.cornerStates[1];
			corners[2] = configuration.cornerStates[5];
			corners[3] = configuration.cornerStates[4];
			break;
		case 1:
			corners[0] = configuration.cornerStates[1];
			corners[1] = configuration.cornerStates[2];
			corners[2] = configuration.cornerStates[6];
			corners[3] = configuration.cornerStates[5];
			break;
		case 2:
			corners[0] = configuration.cornerStates[2];
			corners[1] = configuration.cornerStates[3];
			corners[2] = configuration.cornerStates[7];
			corners[3] = configuration.cornerStates[6];
			break;
		case 3:
			corners[0] = configuration.cornerStates[3];
			corners[1] = configuration.cornerStates[0];
			corners[2] = configuration.cornerStates[4];
			corners[3] = configuration.cornerStates[7];
			break;
		case 4: 
			corners[0] = configuration.cornerStates[3];
			corners[1] = configuration.cornerStates[2];
			corners[2] = configuration.cornerStates[1];
			corners[3] = configuration.cornerStates[0];
			break;
		case 5:
			corners[0] = configuration.cornerStates[4];
			corners[1] = configuration.cornerStates[5];
			corners[2] = configuration.cornerStates[6];
			corners[3] = configuration.cornerStates[7];
			break;
		default:
			throw new IllegalArgumentException();
		}

		int caseIndex = 0;
		if(corners[0]) caseIndex |= 0b0001;
		if(corners[1]) caseIndex |= 0b0010;
		if(corners[2]) caseIndex |= 0b0100;
		if(corners[3]) caseIndex |= 0b1000;

		byte[] edgeNumbers = Tables.INTERSECTION_EDGE_TABLE[caseIndex];

		int[] segmentEdges = new int[edgeNumbers.length];
		for(int i = 0; i < edgeNumbers.length; i++) {
			segmentEdges[i] = edges[edgeNumbers[i]];
		}

		switch(segmentEdges.length) {
		case 0:
			//No segments
			return;
		case 2:
			//No ambiguity
			faceSegments.add(Collections.singletonList(new Segment(segmentEdges[0], segmentEdges[1])));
			break;
		case 4:
			//Ambiguity, return both cases
			faceSegments.add(Arrays.asList(
					new Segment(segmentEdges[0], segmentEdges[1]),
					new Segment(segmentEdges[2], segmentEdges[3])
					));
			faceSegments.add(Arrays.asList(
					new Segment(segmentEdges[2], segmentEdges[1]),
					new Segment(segmentEdges[0], segmentEdges[3])
					));
			break;
		default:
			throw new IllegalStateException();
		}
	}
}
