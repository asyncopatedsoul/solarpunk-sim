using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manager class for handling web views embedded in Unity
/// </summary>
public class WebViewManager : MonoBehaviour
{
    [Header("WebView Settings")]
    [SerializeField] private string baseURL = "http://localhost:3000";
    [SerializeField] private bool allowFileAccess = true;
    
    [Header("Display Settings")]
    [SerializeField] private bool autoResize = true;
    [SerializeField] private Vector2 defaultResolution = new Vector2(1280, 720);
    
    [Header("References")]
    [SerializeField] private RectTransform webViewContainer;
    
    // Cache of active web views
    private Dictionary<string, UnityWebView> activeWebViews = new Dictionary<string, UnityWebView>();
    
    // Singleton instance
    private static WebViewManager instance;
    public static WebViewManager Instance => instance;
    
    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// Create a new web view with the given ID
    /// </summary>
    public UnityWebView CreateWebView(string viewId, RectTransform container = null)
    {
        // Check if web view already exists
        if (activeWebViews.ContainsKey(viewId))
        {
            Debug.LogWarning($"Web view with ID {viewId} already exists. Returning existing instance.");
            return activeWebViews[viewId];
        }
        
        // Use provided container or default
        RectTransform targetContainer = container != null ? container : webViewContainer;
        
        if (targetContainer == null)
        {
            Debug.LogError("No container provided for web view and no default container set.");
            return null;
        }
        
        // Create game object for web view
        GameObject webViewObject = new GameObject($"WebView_{viewId}");
        webViewObject.transform.SetParent(targetContainer, false);
        
        // Set up rect transform
        RectTransform rectTransform = webViewObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Add web view component
        UnityWebView webView = webViewObject.AddComponent<UnityWebView>();
        webView.Initialize(viewId, baseURL, allowFileAccess);
        
        // Add to active web views
        activeWebViews.Add(viewId, webView);
        
        return webView;
    }
    
    /// <summary>
    /// Get an existing web view by ID
    /// </summary>
    public UnityWebView GetWebView(string viewId)
    {
        if (activeWebViews.TryGetValue(viewId, out UnityWebView webView))
        {
            return webView;
        }
        
        return null;
    }
    
    /// <summary>
    /// Destroy a web view by ID
    /// </summary>
    public void DestroyWebView(string viewId)
    {
        UnityWebView webView = GetWebView(viewId);
        
        if (webView != null)
        {
            // Remove from active web views
            activeWebViews.Remove(viewId);
            
            // Destroy game object
            Destroy(webView.gameObject);
        }
    }
    
    /// <summary>
    /// Load a URL in a web view
    /// </summary>
    public void LoadURL(string viewId, string url)
    {
        UnityWebView webView = GetWebView(viewId);
        
        if (webView != null)
        {
            webView.LoadURL(url);
        }
        else
        {
            Debug.LogWarning($"Web view with ID {viewId} not found. Cannot load URL.");
        }
    }
    
    /// <summary>
    /// Execute JavaScript in a web view
    /// </summary>
    public void ExecuteJavaScript(string viewId, string script)
    {
        UnityWebView webView = GetWebView(viewId);
        
        if (webView != null)
        {
            webView.ExecuteJavaScript(script);
        }
        else
        {
            Debug.LogWarning($"Web view with ID {viewId} not found. Cannot execute JavaScript.");
        }
    }
    
    /// <summary>
    /// Create a display web view for a robot face
    /// </summary>
    public UnityWebView CreateRobotFaceWebView(string robotId, RectTransform container)
    {
        string viewId = $"robot_face_{robotId}";
        UnityWebView webView = CreateWebView(viewId, container);
        
        if (webView != null)
        {
            webView.LoadURL($"{baseURL}/display-simulation/robot-face?robotId={robotId}");
        }
        
        return webView;
    }
    
    /// <summary>
    /// Create a control panel web view
    /// </summary>
    public UnityWebView CreateControlPanelWebView(RectTransform container)
    {
        string viewId = "control_panel";
        UnityWebView webView = CreateWebView(viewId, container);
        
        if (webView != null)
        {
            webView.LoadURL($"{baseURL}/remote-control");
        }
        
        return webView;
    }
    
    /// <summary>
    /// Create a code editor web view
    /// </summary>
    public UnityWebView CreateCodeEditorWebView(RectTransform container)
    {
        string viewId = "code_editor";
        UnityWebView webView = CreateWebView(viewId, container);
        
        if (webView != null)
        {
            webView.LoadURL($"{baseURL}/remote-control/editor");
        }
        
        return webView;
    }
    
    /// <summary>
    /// Create a status overlay web view
    /// </summary>
    public UnityWebView CreateStatusOverlayWebView(RectTransform container)
    {
        string viewId = "status_overlay";
        UnityWebView webView = CreateWebView(viewId, container);
        
        if (webView != null)
        {
            webView.LoadURL($"{baseURL}/display-simulation/status-overlay");
        }
        
        return webView;
    }
}

/// <summary>
/// Component for handling a web view in Unity
/// </summary>
public class UnityWebView : MonoBehaviour, WebViewDisplay
{
    private string viewId;
    private string baseURL;
    private bool allowFileAccess;
    
    // WebView native implementation would go here
    // This is a simplified version for the project structure
    
    /// <summary>
    /// Initialize the web view
    /// </summary>
    public void Initialize(string id, string baseUrl, bool allowFiles)
    {
        viewId = id;
        baseURL = baseUrl;
        allowFileAccess = allowFiles;
        
        // Initialize native web view
        // This would be implemented with a platform-specific plugin
        Debug.Log($"Web view {viewId} initialized");
    }
    
    /// <summary>
    /// Load a URL in the web view
    /// </summary>
    public void LoadURL(string url)
    {
        // Handle relative URLs
        if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("file://"))
        {
            url = $"{baseURL}/{url.TrimStart('/')}";
        }
        
        // Load URL in native web view
        // This would be implemented with a platform-specific plugin
        Debug.Log($"Web view {viewId} loading URL: {url}");
    }
    
    /// <summary>
    /// Execute JavaScript in the web view
    /// </summary>
    public void ExecuteJavaScript(string script)
    {
        // Execute JavaScript in native web view
        // This would be implemented with a platform-specific plugin
        Debug.Log($"Web view {viewId} executing JavaScript");
    }
    
    /// <summary>
    /// Handle messages from the web view
    /// </summary>
    private void HandleMessage(string message)
    {
        try
        {
            // Parse message from web view
            // This would handle communication from the web app to Unity
            Debug.Log($"Web view {viewId} received message: {message}");
            
            // TODO: Implement message handling
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling web view message: {ex.Message}");
        }
    }
    
    private void OnDestroy()
    {
        // Clean up native web view
        // This would be implemented with a platform-specific plugin
        Debug.Log($"Web view {viewId} destroyed");
    }
}
