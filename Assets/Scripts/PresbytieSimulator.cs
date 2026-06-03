using System;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.VideoioModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.UnityUtils;
using System.Collections.Generic;


public class PresbytieSimulator : MonoBehaviour
{

    private Mat depthMap;
    private bool depthMapReady = false;
    private Mat destinationMat;
    public bool isActive = true;
    [Header("Fichier modèle ONNX")]
    public string onnxFileName = "midas.onnx"; 
    [Header("Vidéo à lire")]
    public string videoFileName = "video.mp4"; 

    [Header("UI")]
    public RawImage outputImage;
    private Net net;
    private VideoCapture videoCapture;
    private Mat inputBlob;
    private Mat frame;
    private Texture2D outputTexture;

    private const int inputWidth = 384;
    private const int inputHeight = 384;


    void Start()
    {
        // Activer le mode débogage pour OpenCVForUnity
        Utils.setDebugMode(true);

        // Charger le modèle ONNX depuis StreamingAssets
        string modelPath = System.IO.Path.Combine(Application.dataPath, "modele/" + onnxFileName);
        net = Dnn.readNetFromONNX(modelPath);
        if (net.empty())
        {
            Debug.LogError("Échec de chargement du modèle ONNX.");
            return;
        }
        Debug.Log("Modèle ONNX chargé avec succès : " + modelPath);

        // Charger la vidéo
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "OpenCVForUnity/" + videoFileName);
        videoCapture = new VideoCapture(videoPath);
        if (!videoCapture.isOpened())
        {
            Debug.LogError("Échec de l'ouverture de la vidéo : " + videoPath);
            return;
        }
        Debug.Log("Vidéo chargée avec succès : " + videoPath);

        // Initialiser la matrice pour les frames
        frame = new Mat();

        // Créer une texture pour afficher les frames dans l'interface utilisateur
        int videoWidth = (int)videoCapture.get(Videoio.CAP_PROP_FRAME_WIDTH);
        int videoHeight = (int)videoCapture.get(Videoio.CAP_PROP_FRAME_HEIGHT);
        outputTexture = new Texture2D(videoWidth, videoHeight, TextureFormat.RGBA32, false);

        // Associer la texture au composant RawImage de l'interface utilisateur
        RawImage rawImage = GetComponent<RawImage>();
        if (rawImage != null)
        {
            rawImage.texture = outputTexture;
            //Debug.Log("Texture associée au RawImage avec succès.");
        }
        else
        {
            Debug.LogWarning("Aucun composant RawImage trouvé sur ce GameObject.");
        }

        Debug.Log("Initialisation terminée.");
    }


// 2025-08-05 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

void Update()
{
    if (videoCapture == null || !videoCapture.isOpened())
    {
        Debug.LogWarning("La vidéo n'est pas disponible ou n'a pas pu être ouverte.");
        return;
    }

    if (frame == null)
    {
        Debug.LogError("La matrice frame n'est pas initialisée.");
        return;
    }

    if (!videoCapture.read(frame) || frame.empty())
    {
        Debug.LogWarning("Impossible de lire une nouvelle image de la vidéo.");
        return;
    }

    if (net == null)
    {
        Debug.LogError("Le modèle ONNX (net) n'est pas chargé.");
        return;
    }

    if (outputImage == null)
    {
        Debug.LogError("Le composant RawImage (outputImage) n'est pas assigné.");
        return;
    }
    

    try
    {
        // --- Prétraitement pour MiDaS ---
        Mat resized = new Mat();
        Imgproc.resize(frame, resized, new Size(inputWidth, inputHeight));
        Imgproc.cvtColor(resized, resized, Imgproc.COLOR_BGR2RGB);
        resized.convertTo(resized, CvType.CV_32F, 1.0 / 255.0);

        inputBlob = Dnn.blobFromImage(resized, 1.0, new Size(inputWidth, inputHeight), new Scalar(0, 0, 0), true, false);
        net.setInput(inputBlob);

        Mat output = net.forward();
        if (output.empty())
        {
            Debug.LogError("La sortie du modèle ONNX est vide.");
            return;
        }

        int height = output.size(2);
        int width = output.size(3);
        depthMap = new Mat(height, width, CvType.CV_32F);
        output.reshape(1, new int[] { height, width }).copyTo(depthMap);

        Core.normalize(depthMap, depthMap, 0, 1, Core.NORM_MINMAX);

        // --- Redimensionner la carte de profondeur à la taille de la frame vidéo ---
        Mat resizedDepthMap = new Mat();
        Imgproc.resize(depthMap, resizedDepthMap, frame.size());

        // --- Appliquer le flou selon la profondeur ---
        Mat floutedFrame = ApplyPresbytieWithDepth(frame, resizedDepthMap);

        // --- Convertir en RGBA pour Unity ---
        Mat displayMat = new Mat();
        Imgproc.cvtColor(floutedFrame, displayMat, Imgproc.COLOR_BGR2RGBA);

        // --- Mise à jour ou création de la texture ---
        if (outputTexture == null || outputTexture.width != displayMat.cols() || outputTexture.height != displayMat.rows())
        {
            outputTexture = new Texture2D(displayMat.cols(), displayMat.rows(), TextureFormat.RGBA32, false);
            outputImage.texture = outputTexture;
            Debug.Log("Texture recréée pour correspondre à la taille de la matrice.");
        }

        // --- Afficher l’image floutée dans l’UI ---
        Utils.matToTexture2D(displayMat, outputTexture);

        // --- Libération mémoire mats temporaires ---
        resized.release();
        output.release();
        resizedDepthMap.release();
        floutedFrame.release();
        displayMat.release();

        Debug.Log("Affichage mis à jour avec flou selon profondeur.");
    }
    catch (Exception e)
    {
        Debug.LogError("Une erreur s'est produite dans Update : " + e.Message);
    }
}

public Mat ApplyPresbytieWithDepth(Mat input, Mat depthMap)
{
    if (input == null || input.empty() || depthMap == null || depthMap.empty())
        return input;

    // Vérifier tailles
    if (input.size() != depthMap.size())
    {
        Imgproc.resize(depthMap, depthMap, input.size());
        Debug.Log($"DepthMap redimensionné à : {depthMap.width()}x{depthMap.height()}");
    }

    // Normaliser depthMap entre 0-1
    Mat depthNorm = new Mat();
    Core.normalize(depthMap, depthNorm, 0, 1, Core.NORM_MINMAX, CvType.CV_32F);

    // Créer un mask 3 canaux
    Mat mask = new Mat();
    List<Mat> channels = new List<Mat> { depthNorm, depthNorm, depthNorm };
    Core.merge(channels, mask);

    // Conversion en float32 (0-1)
    Mat inputFloat = new Mat();
    input.convertTo(inputFloat, CvType.CV_32FC3, 1.0 / 255.0);

    // Image floutée
    Mat blurred = new Mat();
    Imgproc.GaussianBlur(input, blurred, new Size(25, 25), 0);
    Mat blurredFloat = new Mat();
    blurred.convertTo(blurredFloat, CvType.CV_32FC3, 1.0 / 255.0);

    // Vérif debug : types
    Debug.Log($"Types => inputFloat: {inputFloat.type()}, blurredFloat: {blurredFloat.type()}, mask: {mask.type()}");

    // Fusion avec masque
    Mat sharpPart = new Mat();
    Mat blurredPart = new Mat();
    //Core.multiply(inputFloat, mask, sharpPart);        // proche → net
    //Core.multiply(blurredFloat, Scalar.all(1.0) - mask, blurredPart); // loin → flou
	// Appliquer net sur les zones loin
	Core.multiply(inputFloat, Scalar.all(1.0) - mask, sharpPart);

	// Appliquer flou sur les zones proches
	Core.multiply(blurredFloat, mask, blurredPart);
    Mat outputFloat = new Mat();
    Core.add(sharpPart, blurredPart, outputFloat);

    // Clamp valeurs
    Core.min(outputFloat, new Scalar(1.0, 1.0, 1.0), outputFloat);
    Core.max(outputFloat, new Scalar(0.0, 0.0, 0.0), outputFloat);

    // Debug : min/max par canal
    Core.MinMaxLocResult r = Core.minMaxLoc(depthNorm);
    Debug.Log($"DepthNorm min: {r.minVal}, max: {r.maxVal}");

    List<Mat> checkCh = new List<Mat>();
    Core.split(outputFloat, checkCh);
    for (int i = 0; i < checkCh.Count; i++)
    {
        Core.MinMaxLocResult res = Core.minMaxLoc(checkCh[i]);
        Debug.Log($"Canal {i} => min: {res.minVal}, max: {res.maxVal}");
    }

    // Retour en 8UC3 pour affichage
    Mat output = new Mat();
    outputFloat.convertTo(output, CvType.CV_8UC3, 255.0);

    return output;
}





public void ProcessFrame(Mat currentFrame) 
{
    if (net == null || currentFrame == null || currentFrame.empty())
        return;

    // Prétraitement : redimensionner et normaliser
    Mat resized = new Mat(); 
    Imgproc.resize(currentFrame, resized, new Size(inputWidth, inputHeight));
    Imgproc.cvtColor(resized, resized, Imgproc.COLOR_BGR2RGB);
    
    resized.convertTo(resized, CvType.CV_32F, 1.0 / 255.0); 
    Core.subtract(resized, new Scalar(0.5, 0.5, 0.5), resized); 
    Core.divide(resized, new Scalar(0.5, 0.5, 0.5), resized);
    
    inputBlob = Dnn.blobFromImage(resized); // shape: [1,3,H,W]
    net.setInput(inputBlob); 
    Mat output = net.forward();
    
    depthMap = new Mat(output.size(2), output.size(3), CvType.CV_32F);
    output.reshape(1, new int[] { output.size(2), output.size(3) }).copyTo(depthMap);
    
    Core.normalize(depthMap, depthMap, 0, 1, Core.NORM_MINMAX);
    depthMapReady = true;
}




     public Mat GetDepthMap()
     {
         return depthMap;
     }

     public bool IsDepthMapReady()
     {
         return depthMap != null && !depthMap.empty();
     }


    public void DisablePresbytie()
    {
        isActive = false;
    }

    public void EnablePresbytie()
    {
        isActive = true;
    }

    void OnDestroy()
    {
        videoCapture?.release();
        net?.Dispose();
        frame?.release();
        inputBlob?.release();
    }



}
