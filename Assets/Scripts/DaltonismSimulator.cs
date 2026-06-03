/*using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.VideoioModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;
using System.Collections.Generic;
using OpenCVForUnity.DnnModule;


/// <summary>
/// Script pour la simulation du daltonisme en utilisant OpenCVForUnity et les matrices de transformation.
/// </summary>
public class DaltonismSimulator : MonoBehaviour
{

    public PresbytieSimulator presbytieSimulator;

    [SerializeField] string videoFile = "opencvforunity/video.mp4";
    public int daltonismMode = 0;
    private RawImage rawImage; 

    private VideoCapture video;
    private Mat frame;
    private Texture2D texture;

    // Pour la profondeur
    private Mat depthMap;
    public bool enablePresbytie = false;



    /// <summary>
    /// Initialisation de la vidéo et configuration de la texture.
    /// </summary>
    void Start()
    {

       Utils.setDebugMode(true);
       
       string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFile);
            Debug.Log("Chemin vidéo : " + fullPath);

        video = new VideoCapture(fullPath);
        if (!video.isOpened())
        {
            Debug.LogError("Impossible d'ouvrir la vidéo : " + fullPath);
            return;
        }
        rawImage = GetComponent<RawImage>(); // Assigner le composant RawImage de l'UI
        frame = new Mat();
        texture = new Texture2D((int)video.get(Videoio.CAP_PROP_FRAME_WIDTH),
                                (int)video.get(Videoio.CAP_PROP_FRAME_HEIGHT),
                                TextureFormat.RGBA32, false);


        rawImage.texture = texture; // Assigner la texture à l'UI RawImage

    }

    /// <summary>
    /// Mise à jour : Lecture de la vidéo et application du filtre de daltonisme.
    /// </summary>
   void Update()
   {

       if (!video.read(frame) || frame.empty())
       {
           // Remise à zéro + petit délai pour forcer la relance correcte
           video.set(Videoio.CAP_PROP_POS_FRAMES, 0);
           video.read(frame); // Relire la première frame
       }

       // Étape 1 : effet daltonisme (si activé)
       Mat processedFrame = ApplyDaltonismEffect(frame, daltonismMode);

       // Étape 2 : effet presbytie basé sur la profondeur (MiDaS)
       if (enablePresbytie && presbytieSimulator != null)
       {
           presbytieSimulator.ProcessFrame(processedFrame); // << Ajouté
           if (presbytieSimulator.IsDepthMapReady())
           {
               Mat depth = presbytieSimulator.GetDepthMap();
               processedFrame = presbytieSimulator.ApplyPresbytieWithDepth(processedFrame, depth);
           }
           else
           {
               Debug.LogWarning("Depth map not ready yet.");
           }
       }



       int width = processedFrame.cols();
       int height = processedFrame.rows();

       if (width > 0 && height > 0)
       {
           if (texture == null || texture.width != width || texture.height != height)
           {
               texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
               rawImage.texture = texture;
           }

           // Étape 3 : afficher la frame traitée
           Utils.matToTexture2D(processedFrame, texture);
           rawImage.texture = texture;
       }
       else
       {
           Debug.LogWarning($"Matrice d’image invalide (largeur={width}, hauteur={height}) - texture non créée.");
       }
   }

    
    /// <summary>
    /// https://mk.bcgsc.ca/colorblind/math.mhtml
    /// Applique une transformation des couleurs pour simuler le daltonisme en utilisant des matrices validées.
    /// </summary>
    Mat ApplyDaltonismEffect(Mat inputFrame, int mode)
    {
        Mat rgbFrame = new Mat();
        Imgproc.cvtColor(inputFrame, rgbFrame, Imgproc.COLOR_BGR2RGB);
        Mat outputFrame = new Mat(rgbFrame.size(), rgbFrame.type());

        float[,] matrixData = new float[3, 3];

        switch (mode)
        {
            case 1:
                Debug.Log("Simulation de la Protanopie");
                matrixData = new float[,] {
                    {0.567f, 0.433f, 0.000f},
                    {0.558f, 0.442f, 0.000f},
                    {0.000f, 0.242f, 0.758f}};
                break;

            case 2:
                Debug.Log("Simulation de la Deutéranopie");
                matrixData = new float[,] {
                    {0.625f, 0.375f, 0.000f},
                    {0.700f, 0.300f, 0.000f},
                    {0.000f, 0.300f, 0.700f}};
                break;

            case 3:
                Debug.Log("Simulation de la Tritanopie");
                matrixData = new float[,] {
                    {0.950f, 0.050f, 0.000f},
                    {0.000f, 0.433f, 0.567f},
                    {0.000f, 0.475f, 0.525f}};
                break;
            case 4: // Visual Snow Syndrome
                return ApplyVisualSnow(rgbFrame);
            
            case 5: // Presbytie
                Debug.Log("Simulation de la Presbytie");
                if (presbytieSimulator != null && presbytieSimulator.isActive)
                {
                    Mat depthMap = presbytieSimulator.GetDepthMap();
                    return presbytieSimulator.ApplyPresbytieWithDepth(rgbFrame, depthMap);
                }
                else
                {
                    return rgbFrame;
                }


            default:
                Debug.Log("Mode Normal appliqué");
                // Convertit BGR en RGB sans transformation de couleurs
                Mat normalFrame = new Mat();
                Imgproc.cvtColor(inputFrame, normalFrame, Imgproc.COLOR_BGR2RGB);
                return normalFrame;

        }

        Mat transformMatrix = new Mat(3, 3, CvType.CV_32F);
        transformMatrix.put(0, 0,
                            matrixData[0, 0], matrixData[0, 1], matrixData[0, 2],
                            matrixData[1, 0], matrixData[1, 1], matrixData[1, 2],
                            matrixData[2, 0], matrixData[2, 1], matrixData[2, 2]);

        Core.transform(rgbFrame, outputFrame, transformMatrix);
        return outputFrame;
    }

    Mat ApplyVisualSnow(Mat input)
    {
        Mat noise = new Mat(input.size(), input.type());
        Core.randn(noise, 0, 50); // bruit aléatoire de moyenne 0 et d'écart type 50

        Mat result = new Mat();
        Core.add(input, noise, result);
        return result;
    }






    /// <summary>
    /// Fonction appelée par l'interface utilisateur pour changer dynamiquement le mode de daltonisme.
    /// </summary>
    public void SetDaltonismMode(int mode)
    {
        daltonismMode = mode;
        Debug.Log("Mode Daltonisme changé : " + mode);
    }

    /// <summary>
    /// Libère la mémoire utilisée par la vidéo à la destruction de l'objet.
    /// </summary>
    void OnDestroy()
    {
        if (video != null && video.isOpened())
        {
            video.release();
        }
    }
}*/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;
using System.Collections.Generic;
using OpenCVForUnity.DnnModule;

/// <summary>
/// Script pour la simulation du daltonisme en utilisant OpenCVForUnity et les matrices de transformation.
/// La lecture vidéo utilise le VideoPlayer natif Unity (plus besoin de VideoCapture OpenCV).
/// </summary>
public class DaltonismSimulator : MonoBehaviour
{
    public PresbytieSimulator presbytieSimulator;

    [SerializeField] string videoFile = "OpenCVForUnity/video.mp4";
    public int daltonismMode = 0;
    private RawImage rawImage;

    // --- Remplacement de VideoCapture par VideoPlayer ---
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private Texture2D texture;
    // ----------------------------------------------------

    // Pour la profondeur
    private Mat depthMap;
    public bool enablePresbytie = false;

    /// <summary>
    /// Initialisation du VideoPlayer et configuration de la texture.
    /// </summary>
    void Start()
    {
        rawImage = GetComponent<RawImage>();

        // Récupère ou ajoute le composant VideoPlayer sur ce GameObject
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();

        // Configuration du VideoPlayer
        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFile);
        Debug.Log("Chemin vidéo : " + fullPath);

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = fullPath;
        videoPlayer.renderMode = VideoRenderMode.APIOnly; // On récupère les frames manuellement
        videoPlayer.isLooping = true;
        videoPlayer.playOnAwake = false;
        videoPlayer.skipOnDrop = true;

        // Lance la préparation, puis démarre la vidéo
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.Prepare();
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("Vidéo prête : " + (int)vp.width + "x" + (int)vp.height);

        // Crée la RenderTexture aux dimensions de la vidéo
        renderTexture = new RenderTexture((int)vp.width, (int)vp.height, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        vp.targetTexture = renderTexture;

        // Crée la Texture2D pour OpenCV
        texture = new Texture2D((int)vp.width, (int)vp.height, TextureFormat.RGBA32, false);
        rawImage.texture = texture;

        vp.Play();
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError("Erreur vidéo : " + message);
    }

    /// <summary>
    /// Mise à jour : lecture de la frame courante et application du filtre de daltonisme.
    /// </summary>
    void Update()
    {
        if (videoPlayer == null || !videoPlayer.isPlaying || renderTexture == null || texture == null)
            return;

        // Copie la frame courante de la RenderTexture vers la Texture2D
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new UnityEngine.Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        // Convertit la Texture2D en Mat OpenCV (RGBA → BGR)
        Mat frame = new Mat(texture.height, texture.width, CvType.CV_8UC4);
        Utils.texture2DToMat(texture, frame);

        Mat bgrFrame = new Mat();
        Imgproc.cvtColor(frame, bgrFrame, Imgproc.COLOR_RGBA2BGR);

        // Étape 1 : effet daltonisme
        Mat processedFrame = ApplyDaltonismEffect(bgrFrame, daltonismMode);

        // Étape 2 : effet presbytie basé sur la profondeur (MiDaS)
        if (enablePresbytie && presbytieSimulator != null)
        {
            presbytieSimulator.ProcessFrame(processedFrame);
            if (presbytieSimulator.IsDepthMapReady())
            {
                Mat depth = presbytieSimulator.GetDepthMap();
                processedFrame = presbytieSimulator.ApplyPresbytieWithDepth(processedFrame, depth);
            }
            else
            {
                Debug.LogWarning("Depth map not ready yet.");
            }
        }

        int width = processedFrame.cols();
        int height = processedFrame.rows();

        if (width > 0 && height > 0)
        {
            if (texture == null || texture.width != width || texture.height != height)
            {
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                rawImage.texture = texture;
            }

            // Étape 3 : afficher la frame traitée
            Utils.matToTexture2D(processedFrame, texture);
            rawImage.texture = texture;
        }
        else
        {
            Debug.LogWarning($"Matrice d'image invalide (largeur={width}, hauteur={height}).");
        }
    }

    /// <summary>
    /// https://mk.bcgsc.ca/colorblind/math.mhtml
    /// Applique une transformation des couleurs pour simuler le daltonisme.
    /// </summary>
    Mat ApplyDaltonismEffect(Mat inputFrame, int mode)
    {
        Mat rgbFrame = new Mat();
        Imgproc.cvtColor(inputFrame, rgbFrame, Imgproc.COLOR_BGR2RGB);
        Mat outputFrame = new Mat(rgbFrame.size(), rgbFrame.type());

        float[,] matrixData = new float[3, 3];

        switch (mode)
        {
            case 1:
                Debug.Log("Simulation de la Protanopie");
                matrixData = new float[,] {
                    {0.567f, 0.433f, 0.000f},
                    {0.558f, 0.442f, 0.000f},
                    {0.000f, 0.242f, 0.758f}};
                break;

            case 2:
                Debug.Log("Simulation de la Deutéranopie");
                matrixData = new float[,] {
                    {0.625f, 0.375f, 0.000f},
                    {0.700f, 0.300f, 0.000f},
                    {0.000f, 0.300f, 0.700f}};
                break;

            case 3:
                Debug.Log("Simulation de la Tritanopie");
                matrixData = new float[,] {
                    {0.950f, 0.050f, 0.000f},
                    {0.000f, 0.433f, 0.567f},
                    {0.000f, 0.475f, 0.525f}};
                break;

            case 4: // Visual Snow Syndrome
                return ApplyVisualSnow(rgbFrame);

            case 5: // Presbytie
                Debug.Log("Simulation de la Presbytie");
                if (presbytieSimulator != null && presbytieSimulator.isActive)
                {
                    Mat depth = presbytieSimulator.GetDepthMap();
                    return presbytieSimulator.ApplyPresbytieWithDepth(rgbFrame, depth);
                }
                else
                {
                    return rgbFrame;
                }

            default:
                Debug.Log("Mode Normal appliqué");
                Mat normalFrame = new Mat();
                Imgproc.cvtColor(inputFrame, normalFrame, Imgproc.COLOR_BGR2RGB);
                return normalFrame;
        }

        Mat transformMatrix = new Mat(3, 3, CvType.CV_32F);
        transformMatrix.put(0, 0,
                            matrixData[0, 0], matrixData[0, 1], matrixData[0, 2],
                            matrixData[1, 0], matrixData[1, 1], matrixData[1, 2],
                            matrixData[2, 0], matrixData[2, 1], matrixData[2, 2]);

        Core.transform(rgbFrame, outputFrame, transformMatrix);
        return outputFrame;
    }

    Mat ApplyVisualSnow(Mat input)
    {
        Mat noise = new Mat(input.size(), input.type());
        Core.randn(noise, 0, 50);
        Mat result = new Mat();
        Core.add(input, noise, result);
        return result;
    }

    /// <summary>
    /// Fonction appelée par l'interface utilisateur pour changer dynamiquement le mode de daltonisme.
    /// </summary>
    public void SetDaltonismMode(int mode)
    {
        daltonismMode = mode;
        Debug.Log("Mode Daltonisme changé : " + mode);
    }

    /// <summary>
    /// Libère les ressources à la destruction de l'objet.
    /// </summary>
    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}