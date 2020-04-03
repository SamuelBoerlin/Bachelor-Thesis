package cms;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

public class Main {
	public static void main(String[] args) {
		Map<Configuration, Configuration> configurationToRotatedBaseEquivalent = new HashMap<>();
		Map<Configuration, Set<Configuration>> baseEquivalentToConfigurations = new HashMap<>();
		Collection<Configuration> baseEquivalents = new HashSet<>();

		loop: for(int i = 0; i < 256; i++) {
			Configuration configuration = Configuration.fromIndex(i);

			if(configuration.index() != i) {
				throw new RuntimeException("Index of configuration is incorrect");
			}

			Set<Configuration> equivalentConfigurations = configuration.rotatedStates();

			for(Configuration rotatedEquivalent : equivalentConfigurations) {
				if(baseEquivalents.contains(rotatedEquivalent)) {
					//Equivalent configuration for this configuration found
					configurationToRotatedBaseEquivalent.put(configuration, rotatedEquivalent);
					continue loop;
				}
			}

			//No existing equivalent configuration found
			baseEquivalents.add(configuration);
			configurationToRotatedBaseEquivalent.put(configuration, configuration);
		}

		//Sort base equivalents
		baseEquivalents = new ArrayList<>(baseEquivalents);
		((List<Configuration>)baseEquivalents).sort((c1, c2) -> Integer.compare(c1.index(), c2.index()));

		for(Configuration configuration : baseEquivalents) {
			configuration.mapping.assertUnrotated();
		}

		//Create equivalent configuration -> equivalent configurations multimap
		for(Configuration configuration : configurationToRotatedBaseEquivalent.keySet()) {
			configuration.mapping.assertUnrotated();

			Configuration equivalent = configurationToRotatedBaseEquivalent.get(configuration);

			Set<Configuration> rotatedConfigurations = baseEquivalentToConfigurations.get(equivalent);
			if(rotatedConfigurations == null) {
				baseEquivalentToConfigurations.put(equivalent, rotatedConfigurations = new HashSet<>());
			}

			rotatedConfigurations.add(configuration);
		}

		//Enumerate equivalent configurations
		Map<Configuration, Integer> equivalentConfigToIndex = new HashMap<>();
		Map<Integer, Configuration> equivalentIndexToConfig = new HashMap<>();
		int equivalentConfigIndex = 0;
		for(Configuration equivalent : baseEquivalents) {
			equivalentConfigToIndex.put(equivalent, equivalentConfigIndex);
			equivalentIndexToConfig.put(equivalentConfigIndex, equivalent);
			equivalentConfigIndex++;
		}

		//Enumerate inverses
		Set<RotationMapping> uniqueRotationMappings = new HashSet<>();
		for(Configuration configuration : configurationToRotatedBaseEquivalent.keySet()) {
			Configuration rotatedEquivalent = configurationToRotatedBaseEquivalent.get(configuration);
			uniqueRotationMappings.add(rotatedEquivalent.mapping);
		}
		if(uniqueRotationMappings.size() != 24) {
			throw new RuntimeException("Number of unique rotation mappings does not equal 24");
		}
		Map<RotationMapping, Integer> rotationMappingToIndex = new HashMap<>();
		Map<Integer, RotationMapping> indexToRotationMapping = new HashMap<>();
		int uniqueInverseIndex = 0;
		for(RotationMapping inverse : uniqueRotationMappings) {
			rotationMappingToIndex.put(inverse, uniqueInverseIndex);
			indexToRotationMapping.put(uniqueInverseIndex, inverse);
			uniqueInverseIndex++;
		}

		//Enumerate equivalent configurations
		Map<Configuration, BaseRotationPair> configToPair = new HashMap<>();
		for(Configuration configuration : configurationToRotatedBaseEquivalent.keySet()) {
			Configuration equivalent = configurationToRotatedBaseEquivalent.get(configuration);
			configToPair.put(configuration, new BaseRotationPair(equivalentConfigToIndex.get(equivalent), rotationMappingToIndex.get(equivalent.mapping)));
		}

		//Test inverse mappings
		for(int i = 0; i < 256; i++) {
			Configuration configuration = Configuration.fromIndex(i);

			BaseRotationPair pair = configToPair.get(configuration);

			Configuration rotatedEquivalent = configurationToRotatedBaseEquivalent.get(configuration);

			RotationMapping inverse = indexToRotationMapping.get(pair.rotation);

			//The inverse mapping should bring the rotated indices back to the unrotated
			//indices
			for(int j = 0; j < 8; j++) {
				if(inverse.cornerIndices[configuration.mapping.cornerIndices[j]] != rotatedEquivalent.mapping.cornerIndices[j]) {
					throw new RuntimeException("Edge inverse mapping is incorrect");
				}
			}
			for(int j = 0; j < 12; j++) {
				if(inverse.edgeIndices[configuration.mapping.edgeIndices[j]] != rotatedEquivalent.mapping.edgeIndices[j]) {
					throw new RuntimeException("Edge inverse mapping is incorrect");
				}
			}
			for(int j = 0; j < 6; j++) {
				if(inverse.faceIndices[configuration.mapping.faceIndices[j]] != rotatedEquivalent.mapping.faceIndices[j]) {
					throw new RuntimeException("Edge inverse mapping is incorrect");
				}
			}
		}

		//Map out all components of the base configurations
		Map<Integer, CMSMapper.Component> indexToComponentMap = new HashMap<>();
		Map<CMSMapper.Component, Integer> componentToIndexMap = new HashMap<>();
		Map<Configuration, CMSMapper.CMSResult> equivalentConfigToComponents = new HashMap<>();
		Map<Integer, CMSMapper.CMSResult> equivalentIndexToComponents = new HashMap<>();
		for(Configuration baseEquivalent : baseEquivalents) {
			CMSMapper.CMSResult result = CMSMapper.cms(baseEquivalent, indexToComponentMap, componentToIndexMap);
			equivalentConfigToComponents.put(baseEquivalent, result);
			equivalentIndexToComponents.put(equivalentConfigToIndex.get(baseEquivalent), result);
		}

		for(Configuration configuration : configurationToRotatedBaseEquivalent.keySet()) {
			BaseRotationPair pair = configToPair.get(configuration);

			System.out.println("Configuration: " + configuration.bitString() + ", Index: " + configuration.index() + ", Base index: " + pair.base + ", Rotation index: " + pair.rotation);
		}

		System.out.println();
		System.out.println("---- Base configurations ----");
		System.out.println("Number of base configurations: " + baseEquivalents.size());

		for(Configuration baseEquivalent : baseEquivalents) {
			CMSMapper.CMSResult components = equivalentConfigToComponents.get(baseEquivalent);

			int maxComponentCount = 0;
			for(CMSMapper.CellComponents combination : components.combinations) {
				maxComponentCount = Math.max(maxComponentCount, combination.components.size());
			}

			System.out.println("Base configuration: " + baseEquivalent.bitString() + ", Index: " + equivalentConfigToIndex.get(baseEquivalent) + ", Num active: " + baseEquivalent.numActive() + ", Unique rotations: " + baseEquivalent.rotatedStates().size() + ", Component combinations: " + components.combinations.size() + ", Max component count: " + maxComponentCount);
			System.out.println(baseEquivalent.asciiString());
		}

		System.out.println();
		System.out.println("--- Base components ----");
		System.out.println("Number of unique components: " + indexToComponentMap.size());

		for(int i = 0; i < indexToComponentMap.size(); i++) {
			CMSMapper.Component component = indexToComponentMap.get(i);
			System.out.println("Base component: " + i + ", Vertices: " + component.edges.size());
			System.out.println(component.asciiString());
		}

		System.out.println();
		System.out.println("---- Tables ----");

		int[] rawCaseToCaseRotationAndAmbiguityCountTable = new int[256 * 3];
		for(int i = 0; i < 256; i++) {
			Configuration configuration = Configuration.fromIndex(i);
			BaseRotationPair pair = configToPair.get(configuration);
			rawCaseToCaseRotationAndAmbiguityCountTable[i * 3 + 0] = pair.base;
			rawCaseToCaseRotationAndAmbiguityCountTable[i * 3 + 1] = pair.rotation;
			rawCaseToCaseRotationAndAmbiguityCountTable[i * 3 + 2] = equivalentIndexToComponents.get(pair.base).ambiguousFaces.size();
		}
		System.out.println("constant int RAW_CASE_TO_CASE_ROTATION_AND_AMBIGUITY_COUNT[" + rawCaseToCaseRotationAndAmbiguityCountTable.length + "] = {\n" + printTable(2, rawCaseToCaseRotationAndAmbiguityCountTable, 3) + "};");

		int[] caseAndAmbiguityNrToFaceTable = new int[23 * 6];
		Arrays.fill(caseAndAmbiguityNrToFaceTable, -1);
		for(int i = 0; i < 23; i++) {
			CMSMapper.CMSResult mapping = equivalentIndexToComponents.get(i);

			int ambiguityNr = 0;
			for(int j : mapping.ambiguousFaces) {
				caseAndAmbiguityNrToFaceTable[i * 6 + ambiguityNr] = j;
				ambiguityNr++;
			}
		}
		System.out.println("constant int CASE_AND_AMBIGUITY_NR_TO_FACE[" + caseAndAmbiguityNrToFaceTable.length + "] = {\n" + printTable(2, caseAndAmbiguityNrToFaceTable, 6) + "};");

		int[] caseAndAmbiguityNrToEdgesTable = new int[23 * 6 * 4];
		Arrays.fill(caseAndAmbiguityNrToEdgesTable, -1);
		for(int i = 0; i < 23; i++) {
			CMSMapper.CMSResult mapping = equivalentIndexToComponents.get(i);

			int ambiguityNr = 0;
			for(int j : mapping.ambiguousFaces) {
				int[] edges = new int[4];
				CMSMapper.mapEdges(edges, j);
				
				for(int k = 0; k < 4; k++) {
					caseAndAmbiguityNrToEdgesTable[i * 6 * 4 + ambiguityNr * 4 + k] = edges[k];
				}
				
				ambiguityNr++;
			}
		}
		System.out.println("constant int CASE_AND_AMBIGUITY_NR_TO_EDGES[" + caseAndAmbiguityNrToEdgesTable.length + "] = {\n" + printTable(2, caseAndAmbiguityNrToEdgesTable, 6 * 4) + "};");
		
		int[] caseAndAmbiguityResToSizeAndComponentsTable = new int[23 * 64 * 5];
		Arrays.fill(caseAndAmbiguityResToSizeAndComponentsTable, -1);
		for(int i = 0; i < 23; i++) {
			CMSMapper.CMSResult mapping = equivalentIndexToComponents.get(i);

			int numCombinations = 0;

			for(int j = 0; j < 64; j++) {
				CMSMapper.CellComponents combination = null;
				for(CMSMapper.CellComponents c : mapping.combinations) {
					if(c.ambiguityIndex == j) {
						combination = c;
						break;
					}
				}

				if(combination != null) {
					numCombinations++;

					caseAndAmbiguityResToSizeAndComponentsTable[i * 64 * 5 + j * 5] = combination.components.size();

					for(int k = 0; k < combination.components.size(); k++) {
						caseAndAmbiguityResToSizeAndComponentsTable[i * 64 * 5 + j * 5 + 1 + k] = combination.components.get(k);
					}
				}
			}

			if(numCombinations != mapping.combinations.size()) {
				throw new IllegalStateException("Cell components ambiguity index did not match");
			}
		}
		System.out.println("constant int CASE_AND_AMBIGUITY_RES_TO_SIZE_AND_COMPONENTS[" + caseAndAmbiguityResToSizeAndComponentsTable.length + "] = {\n" + printTable(2, caseAndAmbiguityResToSizeAndComponentsTable, 64 * 5) + "};");

		int[] rotationToSignedEdgesTable = new int[24 * 12];
		for(int i = 0; i < 24; i++) {
			RotationMapping inverse = indexToRotationMapping.get(i);
			for(int j = 0; j < 12; j++) {
				rotationToSignedEdgesTable[i * 12 + j] = inverse.edgeIndices[j];
			}
		}
		System.out.println("constant int ROTATION_TO_RAW_EDGES[" + rotationToSignedEdgesTable.length + "] = {\n" + printTable(2, rotationToSignedEdgesTable, 12) + "};");

		int[] rotationToRawCornersTable = new int[24 * 8];
		for(int i = 0; i < 24; i++) {
			RotationMapping inverse = indexToRotationMapping.get(i);
			for(int j = 0; j < 8; j++) {
				rotationToRawCornersTable[i * 8 + j] = inverse.cornerIndices[j];
			}
		}
		System.out.println("constant int ROTATION_TO_RAW_CORNERS[" + rotationToRawCornersTable.length + "] = {\n" + printTable(2, rotationToRawCornersTable, 8) + "};");

		int[] rotationToRawFacesTable = new int[24 * 6];
		for(int i = 0; i < 24; i++) {
			RotationMapping inverse = indexToRotationMapping.get(i);
			for(int j = 0; j < 6; j++) {
				rotationToRawFacesTable[i * 6 + j] = inverse.faceIndices[j];
			}
		}
		System.out.println("constant int ROTATION_TO_RAW_FACES[" + rotationToRawFacesTable.length + "] = {\n" + printTable(2, rotationToRawFacesTable, 6) + "};");

		int[] componentToEdgesTable = new int[indexToComponentMap.size() * 13];
		for(int i = 0; i < indexToComponentMap.size(); i++) {
			CMSMapper.Component component = indexToComponentMap.get(i);

			componentToEdgesTable[i * 13] = component.edges.size();

			for(int j = 0; j < 12; j++) {
				componentToEdgesTable[i * 13 + 1 + j] = j < component.edges.size() ? component.edges.get(j) : -1;
			}
		}
		System.out.println("constant int COMPONENT_TO_SIZE_AND_EDGES[" + componentToEdgesTable.length + "] = {\n" + printTable(2, componentToEdgesTable, 13) + "};");

		int[] edgePairToFaceTable = new int[12 * 12];
		for(int i = 0; i < 12; i++) {
			for(int j = 0; j < 12; j++) {

				if(i != j) {
					int face = -1;

					for(int k = 0; k < 6; k++) {
						byte[] faceEdges = Tables.FACE_TO_EDGES_TABLE[k];

						boolean hasEdgeI = false;
						boolean hasEdgeJ = false;

						for(byte edge : faceEdges) {
							if(edge == i) {
								hasEdgeI = true;
							}
							if(edge == j) {
								hasEdgeJ = true;
							}
						}

						if(hasEdgeI && hasEdgeJ) {
							if(face != -1) {
								throw new RuntimeException("Edge pair mapped to multiple faces");
							}
							face = k;
						}
					}

					edgePairToFaceTable[i * 12 + j] = face;
				} else {
					edgePairToFaceTable[i * 12 + j] = -1;
				}
			}
		}
		System.out.println("constant int EDGE_PAIR_TO_FACE[" + edgePairToFaceTable.length + "] = {\n" + printTable(2, edgePairToFaceTable, 12) + "};");

		System.out.println("Total table size: " + ((rawCaseToCaseRotationAndAmbiguityCountTable.length + caseAndAmbiguityResToSizeAndComponentsTable.length + rotationToSignedEdgesTable.length + rotationToRawCornersTable.length + rotationToRawFacesTable.length + componentToEdgesTable.length + caseAndAmbiguityNrToFaceTable.length + edgePairToFaceTable.length) / 1000.0f) + "KB");
	}

	private static String printTable(int indent, int[] array, int stride) {
		String str = "";

		for(int y = 0; y < array.length / stride; y++) {
			for(int i = 0; i < indent; i++) {
				str += " ";
			}

			for(int x = 0; x < stride; x++) {
				int index = y * stride + x;

				if(index < array.length) {
					str += array[index];
				}

				if(index != array.length - 1) {
					str += ", ";
				}
			}

			str += "\n";
		}

		return str;
	}
}
