using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class QueryResultDisplay : MonoBehaviour
{
    [SerializeField] private GameObject queryTilePrefab;

    [SerializeField] private VRSculpting sculpting;

    [SerializeField] private Transform positionTransform;

    [SerializeField] private int topBottomRows = 2;

    [SerializeField] private float xSpacingAngle = 10.0f;
    [SerializeField] private float ySpacingAngle = 10.0f;

    [SerializeField] private float radius = 0.8f;

    [SerializeField] private GameObject loadingRenderer;

    private int currentQueryId = -1;

    private List<GameObject> tiles = new List<GameObject>();
    private List<GameObject> queryResultObjects = new List<GameObject>();

    private void Start()
    {
        loadingRenderer.SetActive(false);
    }

    private void LateUpdate()
    {
        transform.position = positionTransform.position;
    }

    public void PrepareNewQuery(int queryId)
    {
        //Remove all previous results
        ResetDisplay();
        currentQueryId = queryId;

        //Face user
        var camForward = sculpting.UserCamera.transform.forward;
        transform.rotation = Quaternion.LookRotation(new Vector3(camForward.x, 0, camForward.z).normalized, Vector3.up);

        //Show loading screen
        loadingRenderer.SetActive(true);
    }

    public void FinishQuery(int queryId, Exception ex)
    {
        if (currentQueryId == -1 || currentQueryId == queryId)
        {
            loadingRenderer.SetActive(false);
        }

        if (ex != null)
        {
            Debug.LogError("An error has occurred during the query");
            Debug.LogError(ex.Message);
        }
    }

    public void ResetDisplay()
    {
        foreach (var tile in tiles)
        {
            Destroy(tile);
        }
        tiles.Clear();

        foreach (var obj in queryResultObjects)
        {
            Destroy(obj);
        }
        queryResultObjects.Clear();

        currentQueryId = -1;

        loadingRenderer.SetActive(false);
    }

    public void SetQueryResult(int queryId, int scoreIndex, UnityCineastApi.QueryResult result, GameObject go, QueryResultObject queryResultObjectScript)
    {
        if (queryId == currentQueryId)
        {
            queryResultObjectScript.ObjectLabel.Camera = sculpting.UserCamera;

            loadingRenderer.SetActive(false);

            queryResultObjects.Add(go);

            var objectToTextureRenderManager = GameObject.FindGameObjectWithTag("ObjectToTextureRenderManager").GetComponent<ObjectToTextureRenderManager>();

            var tile = Instantiate(queryTilePrefab, transform);

            tiles.Add(tile);

            float xAngle = (float)Math.PI / 2.0f;
            float yAngle = 0.0f;

            int rows = topBottomRows * 2 + 1;

            int rowIndex = scoreIndex % rows;
            if (rowIndex > 0)
            {
                if (rowIndex % 2 == 0)
                {
                    yAngle -= (rowIndex / 2) * Mathf.Deg2Rad * ySpacingAngle;
                }
                else
                {
                    yAngle += (rowIndex / 2 + 1) * Mathf.Deg2Rad * ySpacingAngle;
                }
            }

            int colIndex = scoreIndex / rows;
            if (colIndex > 0)
            {
                if (colIndex % 2 == 0)
                {
                    xAngle -= (colIndex / 2) * Mathf.Deg2Rad * xSpacingAngle;
                }
                else
                {
                    xAngle += (colIndex / 2 + 1) * Mathf.Deg2Rad * xSpacingAngle;
                }
            }

            float xcos = Mathf.Cos(xAngle);
            float xsin = Mathf.Sin(xAngle);

            float ycos = Mathf.Cos(yAngle);
            float ysin = Mathf.Sin(yAngle);

            Vector3 offset = Vector3.right * xcos * ycos * radius + Vector3.forward * xsin * ycos * radius + Vector3.up * ysin * radius;

            tile.transform.localPosition = offset;
            tile.transform.localRotation = Quaternion.LookRotation(offset.normalized, Vector3.up);

            var ui = tile.GetComponent<VRUI>();
            ui.InitializeUI(sculpting, sculpting.InputModule, sculpting.EventCamera);

            var queryResultTile = tile.GetComponent<QueryResultTile>();

            queryResultTile.ObjectPreview.ObjectRenderer = objectToTextureRenderManager;
            queryResultTile.ObjectPreview.RenderObject = go;
        }
        else
        {
            Destroy(go);
        }
    }

    private void OnDestroy()
    {
        ResetDisplay();
    }
}
