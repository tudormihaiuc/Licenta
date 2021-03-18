using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Data : MonoBehaviour
{
    public static void SaveProfile(ProfileData t_profile)
    {
        try
        {
            string path = Application.persistentDataPath + "/Profile.data";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            FileStream file = File.Create(path);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(file, t_profile);
            file.Close();
            Debug.Log("Saved Successfully");
        }catch{
            Debug.Log("Something went wrong with the data file");
        }
    }

    public static ProfileData LoadProfile()
    {
        ProfileData returnedValue = new ProfileData();
        try
        {
            string path = Application.persistentDataPath + "/Profile.data";
            if (File.Exists(path))
            {
                FileStream file = File.Open(path, FileMode.Open);
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                returnedValue = (ProfileData)binaryFormatter.Deserialize(file);
                Debug.Log("Loaded Successfully");
            }
        }catch{
            Debug.Log("Something went wrong when loading or file wasn't found ");
        }
        return returnedValue;
    }
}
