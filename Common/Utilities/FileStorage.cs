using System.IO;
using System.Text.Json;

namespace YnabFeeder.Common.Utilities {
    // https://stackoverflow.com/a/22417240/2621693
    public static class FileStorage {
        /// <summary>
        /// Writes the given object instance to a Json file.
        /// <para>Object type must have a parameterless constructor.</para>
        /// <para>Only Public properties and variables will be written to the file. These can be any type though, even other classes.</para>
        /// <para>If there are public properties/variables that you do not want written to the file, decorate them with the [JsonIgnore] attribute.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false) where T : new() {
            var contentsToWriteToFile = JsonSerializer.Serialize(objectToWrite);
            using var writer = new StreamWriter(filePath, append);
            writer.Write(contentsToWriteToFile);
            writer.Close();
        }

        /// <summary>
        /// Reads an object instance from an Json file.
        /// <para>Object type must have a parameterless constructor.</para>
        /// </summary>
        /// <typeparam name="T">The type of object to read from the file.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the Json file.</returns>
        public static T ReadFromJsonFile<T>(string filePath) where T : new() {
            using var reader = new StreamReader(filePath);
            var fileContents = reader.ReadToEnd();
            reader.Close();

            return JsonSerializer.Deserialize<T>(fileContents);
        }
    }
}
