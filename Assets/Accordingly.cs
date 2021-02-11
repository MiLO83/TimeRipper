using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Linq;
static class CanvasExtensions
{
    public static Vector2 SizeToParent(this RawImage image, float padding = 0)
    {
        var parent = image.transform.parent.GetComponent<RectTransform>();
        var imageTransform = image.GetComponent<RectTransform>();
        if (!parent) { return imageTransform.sizeDelta; } //if we don't have a parent, just return our current width;
        padding = 1 - padding;
        float w = 0, h = 0;
        float ratio = image.texture.width / (float)image.texture.height;
        var bounds = new Rect(0, 0, parent.rect.width, parent.rect.height);
        if (Mathf.RoundToInt(imageTransform.eulerAngles.z) % 180 == 90)
        {
            //Invert the bounds if the image is rotated
            bounds.size = new Vector2(bounds.height, bounds.width);
        }
        //Size by height first
        h = bounds.height * padding;
        w = h * ratio;
        if (w > bounds.width * padding)
        { //If it doesn't fit, fallback to width;
            w = bounds.width * padding;
            h = w / ratio;
        }
        imageTransform.sizeDelta = new Vector2(w, h);
        return imageTransform.sizeDelta;
    }
}

public class Accordingly : MonoBehaviour
{
    const string quote = "\"";
    public string dirPath = "";
    public RawImage ri;
    public Texture2D srcTex;
    public Texture2D srcTex256;
    public Texture2D destTex;
    public static Color32[] srcImg;
    public Color32[] srcImg256;
    public Color32[] destImg;
    public Color32[] originalColors;
    public string[] allPNGs;
    public Color32 tempColor;
    public List<int> foundViewNumbers = new List<int>();
    // Start is called before the first frame update
    void Start()
    {
        destTex = new Texture2D(32, 32);
        srcTex = new Texture2D(32, 32);
        StartCoroutine(ProcessBatch());
    }
    private static int GetClosestColor(Color32[] colorArray, Color32 baseColor)
    {
        var colors = colorArray.Select(x => new { Value = x, Diff = GetDiff(x, baseColor) }).ToList();
        var min = colors.Min(x => x.Diff);
        var foundcolor = colors.Find(x => x.Diff == min).Value;
        bool found = false;
        int match = 0;
        for (int i = 0; i < colorArray.Length; i++)
        {
            if (colorArray[i].Equals(foundcolor))
            {
                found = true;
                match = i;

            }
        }
        if (found)
        {
            return match;
        }
        else
        {
            return 0;
        }
    }

    private static float GetDiff(Color32 color, Color32 baseColor)
    {
        int a = color.a - baseColor.a,
            r = color.r - baseColor.r,
            g = color.g - baseColor.g,
            b = color.b - baseColor.b;
        return a * a + r * r + g * g + b * b;
    }
    public IEnumerator ProcessBatch()
    {
        
        srcTex.filterMode = FilterMode.Point;
        destTex.filterMode = FilterMode.Point;
        tempColor = new Color32(255, 255, 255, 255);

        DirectoryInfo di = new DirectoryInfo(Application.dataPath + "/../Input/");
        Directory.CreateDirectory(Application.dataPath + "/../Output/");
        Debug.Log("Looking for Images in : " + di.FullName);
        FileInfo[] files = di.GetFiles("view*.png", SearchOption.TopDirectoryOnly);
        List<FileInfo> view = new List<FileInfo>();
        int f = 0;
        while (f < files.Length-1)
        {

            while (f < files.Length-1 && files[f] != null && files[f + 1] != null && files[f].Name.Split('.')[1] != null && files[f + 1].Name.Split('.')[1] != null && files[f].Name.Split('.')[1] == files[f + 1].Name.Split('.')[1] && files[f].Name.Split('.')[2] != null && files[f + 1].Name.Split('.')[2] != null)
            {
                Debug.Log("Found File : " + files[f].Name);
                view.Add(files[f]);
                f++;
            }
        
            dirPath = "";
            for (int v = 0; v < view.Count - 1; v++)
            {
                if (view[v + 1] != null && view[v].Name.Split('.')[1] == view[v + 1].Name.Split('.')[1] && view[v].Name.Split('.')[2] == view[v + 1].Name.Split('.')[2])
                {
                                        
                        for (int t = 0; t < 4; t++)
                        {
                            yield return new WaitForSeconds(0.5f);
                            string fileName = "-0 " + view[v].FullName + " -1 " + view[v + 1].FullName + " -o " + Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png" + " -s " + (t * 0.25) + " -v";
                            Process p = new Process();
                            Debug.Log("RUNNING : " + Application.dataPath + "/../dain-ncnn/dain-ncnn-vulkan.exe");
                            Debug.Log("Command Arguments : " + fileName);
                            p.StartInfo = new ProcessStartInfo(Application.dataPath + "/../dain-ncnn/dain-ncnn-vulkan.exe", fileName)
                            {
                                WorkingDirectory = Application.dataPath + "/../dain-ncnn/",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            p.Start();

                            string output = p.StandardOutput.ReadToEnd();
                            p.WaitForExit();

                            Debug.Log(output);
                        
                        if (File.Exists(Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png"))
                        {
                            
                            srcTex.LoadImage(File.ReadAllBytes(view[v].FullName));
                            srcTex.Apply();
                            srcImg = srcTex.GetPixels32();
                            destTex.LoadImage(File.ReadAllBytes(Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png"));
                            destTex.Apply();
                            destImg = destTex.GetPixels32();
                            for (int y = 0; y < srcTex.height; y++)
                            {
                                for (int x = 0; x < srcTex.width; x++)
                                {
                                    destImg[(y * srcTex.width) + x] = srcImg[GetClosestColor(srcImg, destImg[(y * srcTex.width) + x])];
                                }
                            }
                            destTex.SetPixels32(destImg);
                            destTex.Apply();
                            byte[] destBytes = destTex.EncodeToPNG();
                            File.WriteAllBytes(Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png", destBytes);
                            ri.texture = destTex;
                            ri.SizeToParent(0);
                        } else
                        {
                            
                            if (File.Exists(Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png"))
                            {
                                srcTex.LoadImage(File.ReadAllBytes(view[v].FullName));
                                srcTex.Apply();
                                srcImg = srcTex.GetPixels32();
                                destTex.LoadImage(File.ReadAllBytes(Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png"));
                                destTex.Apply();
                                destImg = destTex.GetPixels32();
                                for (int y = 0; y < srcTex.height; y++)
                                {
                                    for (int x = 0; x < srcTex.width; x++)
                                    {
                                        destImg[(y * srcTex.width) + x] = srcImg[GetClosestColor(srcImg, destImg[(y * srcTex.width) + x])];
                                    }
                                }
                                destTex.SetPixels32(destImg);
                                destTex.Apply();
                                byte[] destBytes = destTex.EncodeToPNG();
                                File.WriteAllBytes(Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png", destBytes);
                                ri.texture = destTex;
                                ri.SizeToParent(0);
                            }
                        }
                    }
                    
                }
                if (view[v + 1] == null && view[v].Name.Split('.')[1] == view[0].Name.Split('.')[1] && view[v].Name.Split('.')[2] == view[0].Name.Split('.')[2])
                {

                    for (int t = 0; t < 4; t++)
                    {
                        
                        string fileName = "-0 " + view[v].FullName + " -1 " + view[0].FullName + " -o " + Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png" + " -s " + (t * 0.25) + " -v";
                        Process p = new Process();
                        Debug.Log("RUNNING : " + Application.dataPath + "/../dain-ncnn/dain-ncnn-vulkan.exe");
                        Debug.Log("Command Arguments : " + fileName);
                        p.StartInfo = new ProcessStartInfo(Application.dataPath + "/../dain-ncnn/dain-ncnn-vulkan.exe", fileName)
                        {
                            WorkingDirectory = Application.dataPath + "/../dain-ncnn/",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        p.Start();

                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit();

                        Debug.Log(output);

                       
                        if (File.Exists(Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png"))
                        {
                            
                            srcTex.LoadImage(File.ReadAllBytes(view[v].FullName));
                            srcTex.Apply();
                            srcImg = srcTex.GetPixels32();
                            destTex.LoadImage(File.ReadAllBytes(Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png"));
                            destTex.Apply();
                            destImg = destTex.GetPixels32();
                            for (int y = 0; y < srcTex.height; y++)
                            {
                                for (int x = 0; x < srcTex.width; x++)
                                {
                                    destImg[(y * srcTex.width) + x] = srcImg[GetClosestColor(srcImg, destImg[(y * srcTex.width) + x])];
                                }
                            }
                            destTex.SetPixels32(destImg);
                            destTex.Apply();
                            byte[] destBytes = destTex.EncodeToPNG();
                            File.WriteAllBytes(Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png", destBytes);
                            ri.texture = destTex;
                            ri.SizeToParent(0);
                        }
                        else
                        {
                            
                            if (File.Exists(Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png"))
                            {
                                srcTex.LoadImage(File.ReadAllBytes(view[v].FullName));
                                srcTex.Apply();
                                srcImg = srcTex.GetPixels32();
                                destTex.LoadImage(File.ReadAllBytes(Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png"));
                                destTex.Apply();
                                destImg = destTex.GetPixels32();
                                for (int y = 0; y < srcTex.height; y++)
                                {
                                    for (int x = 0; x < srcTex.width; x++)
                                    {
                                        destImg[(y * srcTex.width) + x] = srcImg[GetClosestColor(srcImg, destImg[(y * srcTex.width) + x])];
                                    }
                                }
                                destImg = srcTex.GetPixels32();
                                destTex.Apply();
                                byte[] destBytes = destTex.EncodeToPNG();
                                File.WriteAllBytes(Application.dataPath + "/../Output/view." + view[v].Name.Split('.')[1] + "." + view[v].Name.Split('.')[2] + "." + view[v].Name.Split('.')[3] + ".t." + t + ".template.png", destBytes);
                                ri.texture = destTex;
                                ri.SizeToParent(0);
                            }
                        }
                    }

                }
                

            }
            view.Clear();
            yield return new WaitForSeconds(0.1f);
            f++;
        }
        /*foreach (var fi in di.GetFiles("*.png"))
        {
            if (fi.Name.Split('.')[0] == "view")
            {
                
                    int viewNum = int.Parse(fi.Name.Split('.')[1]);
                    if (!foundViewNumbers.Contains(viewNum))
                    {
                        foundViewNumbers.Add(viewNum);
                    }
                    Debug.Log(fi.Name);
                
            }
        }
        foreach (int vstr in foundViewNumbers.ToArray())
        {
            srcTex = new Texture2D(32, 32);
            srcTex256 = new Texture2D(32, 32);
            srcTex.LoadImage(File.ReadAllBytes(di.FullName + "/pic." + vstr.ToString() + ".png"));
            srcTex.Apply();
            srcTex256.LoadImage(File.ReadAllBytes(di.FullName + "/pic." + vstr.ToString() + "_256.png"));
            srcTex256.Apply();
            destTex = new Texture2D(srcTex.width, srcTex.height);
            destImg = new Color32[srcTex.width * srcTex.height];
            srcImg = srcTex.GetPixels32();
            srcImg256 = srcTex256.GetPixels32();

            if (File.Exists(di.FullName + "/pic." + vstr.ToString() + ".png"))
            {
                Debug.Log("FOUND : " + di.FullName + "/pic." + vstr.ToString() + ".png");

                destTex = new Texture2D(srcTex.width, srcTex.height);
                destImg = new Color32[srcTex.width * srcTex.height];
                for (int y = 0; y < srcTex.height; y++)
                {
                    for (int x = 0; x < srcTex.width; x++)
                    {
                        tempColor.r = (byte)(int)Mathf.Clamp((srcImg[(y * srcTex.width) + x].r - srcImg256[(y * srcTex.width) + x].r), 0, 255);
                        tempColor.g = (byte)(int)Mathf.Clamp((srcImg[(y * srcTex.width) + x].g - srcImg256[(y * srcTex.width) + x].g), 0, 255);
                        tempColor.b = (byte)(int)Mathf.Clamp((srcImg[(y * srcTex.width) + x].b - srcImg256[(y * srcTex.width) + x].b), 0, 255);
                        tempColor.a = (byte)(int)Mathf.Clamp(((tempColor.r + tempColor.g + tempColor.b) / 3), 0, 255);
                        destImg[(y * srcTex.width) + x] = tempColor;
                        if ((srcImg[(y * srcTex.width) + x].r - srcImg256[(y * srcTex.width) + x].r) > 255)
                        {
                            Debug.Log(((srcImg[(y * srcTex.width) + x].r - srcImg256[(y * srcTex.width) + x].r).ToString()));
                        }
                    }
                }
            }
            
            if (destTex == null)
                destTex = new Texture2D(srcTex.width, srcTex.height);
            destTex.SetPixels32(destImg);
            destTex.Apply();
            byte[] bytes = destTex.EncodeToPNG();
            System.IO.File.WriteAllBytes(Application.dataPath + "/../Output/" + "/pic." + vstr + "_o.png", bytes);
            Debug.Log("WROTE : " + Application.dataPath + "/../Output/" + "/pic." + vstr + "_o.png");
            yield return new WaitForEndOfFrame();
        }
        */
        Application.Quit();
    }
    // Update is called once per frame
    void Update()
    {
        ri.texture = destTex;
    }
}
