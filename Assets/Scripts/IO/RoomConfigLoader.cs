using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomConfigLoader : MonoBehaviour
{
    [field: SerializeField] public Transform contentView { get; private set; }
    [field: SerializeField] public ScrollRect scroll { get; private set; }
    [field: SerializeField] public GameObject filePrefab { get; private set; }

    void Awake()
    {
        if (Directory.Exists(Application.dataPath + "/Saved/"))
        {
            string[] files = Directory.GetFiles(Application.dataPath + "/Saved/");
            foreach (string f in files.Where(x => x.EndsWith(".json")))
            {
                GameObject go = Instantiate(filePrefab, Vector3.zero, Quaternion.identity);

                go.transform.SetParent(contentView);
                go.GetComponentInChildren<TMP_Text>().text = Path.GetFileName(f)
                                                            .Replace(".json", "")
                                                            .Replace("_", " ");
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    ConfigurationManager._instance.LoadRoom(f);
                    transform.root.gameObject.SetActive(false);
                });
            }
        }

        gameObject.SetActive(false);
    }
}
