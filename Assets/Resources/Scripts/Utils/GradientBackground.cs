using UnityEngine;

/* 
 * - Just drop it into your main camera and it will automatically draw gradient background.
 * - You don't need to add any object for background drawing, this class will add it himself.
 * - You have to assign top and bottom colours of needed gradient via appropriate parameters.
 * - It's also recommended to assign an appropriate shader for better gradient drawing (..\Assets\Shaders\Grad.shader will be perfect).
 * - Call RefreshColors method after each "top" and "bottom" colours change if you do it from your code.
 */

public class GradientBackground : MonoBehaviour {
	// ============== PUBLIC FIELDS ==============
	public Color topColor = Color.blue;
	public Color bottomColor = Color.white;
	public int gradientLayer = 7;
    public Shader shader; 

	// ============== PRIVATE FIELDS ==============
    private Camera m_camera;
    private Camera m_gradientCamera;
    private GameObject m_gradientPlane;
 
	// ============== LIFETIME ==============
	void Awake() {	
        m_camera = GetComponent<Camera>();

		gradientLayer = Mathf.Clamp(gradientLayer, 0, 31);

        if (!m_camera) {
        	Debug.LogError ("Must attach GradientBackground script to the camera");
        	return;
    	}
 
    	m_camera.clearFlags = CameraClearFlags.Depth;
    	m_camera.cullingMask = m_camera.cullingMask & ~(1 << gradientLayer);

        m_gradientCamera = new GameObject("Gradient Cam", typeof(Camera)).GetComponent<Camera>();
    	m_gradientCamera.depth = m_camera.depth-1;
    	m_gradientCamera.cullingMask = 1 << gradientLayer;

   		RefreshColors();
	}

	// ============== PUBLIC METHODS ============== 
    public void RefreshColors()
    {
		Mesh mesh = new Mesh();

		mesh.vertices = new Vector3[4]{new Vector3(-100f, .577f, 1f), new Vector3(100f, .577f, 1f), new Vector3(-100f, -.577f, 1f), new Vector3(100f, -.577f, 1f)};
 
		mesh.colors = new Color[4]{topColor, topColor, bottomColor, bottomColor};
 
		mesh.triangles = new int[6]{0, 1, 2, 1, 3, 2};
 
		Material mat = new Material(shader);

        if(!m_gradientPlane)
    	    m_gradientPlane = new GameObject("Gradient Plane", typeof(MeshFilter), typeof(MeshRenderer));
 
		((MeshFilter)m_gradientPlane.GetComponent(typeof(MeshFilter))).mesh = mesh;
    	m_gradientPlane.GetComponent<Renderer>().material = mat;
    	m_gradientPlane.layer = gradientLayer;
    }
}