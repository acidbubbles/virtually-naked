using Device = SharpDX.Direct3D11.Device;
using System.Collections.Generic;
using System.Collections.Immutable;

public class FigureRendererLoader {
	private readonly IArchiveDirectory dataDir;
	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly TextureCache textureCache;

	public FigureRendererLoader(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, TextureCache textureCache) {
		this.dataDir = dataDir;
		this.device = device;
		this.shaderCache = shaderCache;
		this.textureCache = textureCache;
	}

	public FigureRenderer Load(IArchiveDirectory figureDir, MaterialSetAndVariantOption materialSetOption) {
		SurfaceProperties surfaceProperties = Persistance.Load<SurfaceProperties>(figureDir.File("surface-properties.dat"));

		var refinementDirectory = figureDir.Subdirectory("refinement");

		var controlMeshDirectory = refinementDirectory.Subdirectory("control");
		int[] surfaceMap = controlMeshDirectory.File("surface-map.array").ReadArray<int>();

		var refinedMeshDirectory = refinementDirectory.Subdirectory("level-" + surfaceProperties.SubdivisionLevel);
		SubdivisionMesh mesh = SubdivisionMeshPersistance.Load(refinedMeshDirectory);
		int[] controlFaceMap = refinedMeshDirectory.File("control-face-map.array").ReadArray<int>();
		
		var materialSet = MaterialSet.LoadActive(device, shaderCache, textureCache, dataDir, figureDir, materialSetOption, surfaceProperties);
		var materials = materialSet.Materials;
		
		Scatterer scatterer = surfaceProperties.PrecomputeScattering ? Scatterer.Load(device, shaderCache, figureDir, materialSetOption.MaterialSet.Label) : null;

		var uvSetName = materials[0].UvSet;
		IArchiveDirectory uvSetsDirectory = figureDir.Subdirectory("uv-sets");
		Quad[] texturedFaces = uvSetsDirectory.File("textured-faces.array").ReadArray<Quad>();
		int[] texturedToSpatialIdxMap = uvSetsDirectory.File("textured-to-spatial-idx-map.array").ReadArray<int>();

		IArchiveDirectory uvSetDirectory = uvSetsDirectory.Subdirectory(uvSetName);

		var texturedVertexInfos = uvSetDirectory.File("textured-vertex-infos.array").ReadArray<TexturedVertexInfo>();
		
		var vertexRefiner = new VertexRefiner(device, shaderCache, mesh, texturedToSpatialIdxMap, texturedVertexInfos);
		
		FigureSurface[] surfaces = FigureSurface.MakeSurfaces(device, materials.Length, texturedFaces, controlFaceMap, surfaceMap, materialSet.FaceTransparencies);
		
		HashSet<int> visitedSurfaceIndices = new HashSet<int>();
		List<int> surfaceOrder = new List<int>(surfaces.Length);
		bool[] areUnorderedTransparent = new bool[surfaces.Length];

		//first add surfaces with an explicity-set render order
		foreach (int surfaceIdx in surfaceProperties.RenderOrder) {
			visitedSurfaceIndices.Add(surfaceIdx);
			surfaceOrder.Add(surfaceIdx);
			areUnorderedTransparent[surfaceIdx] = false;
		}

		//then add any remaining surfaces
		for (int surfaceIdx = 0; surfaceIdx < surfaces.Length; ++surfaceIdx) {
			if (visitedSurfaceIndices.Contains(surfaceIdx)) {
				continue;
			}
			surfaceOrder.Add(surfaceIdx);
			areUnorderedTransparent[surfaceIdx] = true;
		}

		var isOneSided = figureDir.Name == "genesis-3-female"; //hack

		return new FigureRenderer(device, shaderCache, scatterer, vertexRefiner, materialSet, surfaces, isOneSided, surfaceOrder.ToArray(), areUnorderedTransparent);
	}
}
