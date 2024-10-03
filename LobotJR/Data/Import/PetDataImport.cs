using LobotJR.Command.Model.Pets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Data.Import
{
    public static class PetDataImport
    {
        public static readonly string ContentFolderName = "content";
        public static readonly string PetListPath = "petlist.ini";
        public static readonly string PetFolder = "pets";
        public static IFileSystem FileSystem = new FileSystem();

        public static Dictionary<string, int> SeedPetRarity(IRepository<PetRarity> rarityRepository)
        {
            List<string> names = new List<string>()
            {
                "Common", "Uncommon", "Rare", "Epic", "Legendary"
            };
            List<float> drops = new List<float>() { 150f / 2000f, 50f / 2000f, 25f / 2000f, 10f / 2000f, 1f / 2000f };
            for (var i = 0; i < names.Count; i++)
            {
                rarityRepository.Create(new PetRarity() { Name = names[i], DropRate = drops[i] });
            }
            rarityRepository.Commit();
            return rarityRepository.Read().ToDictionary(x => (names.IndexOf(x.Name) + 1).ToString(), x => x.Id);
        }

        public static IEnumerable<Tuple<int, Pet>> LoadPetData(string contentFolderName, string petListPath, string petFolder, Dictionary<string, int> rarityMap)
        {
            try
            {
                var entries = FileSystem.ReadAllLines($"{contentFolderName}/{petListPath}")
                    .Select(x => x.Split(','))
                    .Where(x => x.Length == 2)
                    .ToDictionary(x => x[0], x => x[1]);

                var output = new List<Tuple<int, Pet>>();
                foreach (var entry in entries)
                {
                    var path = $"{contentFolderName}/{petFolder}/{entry.Value}";
                    var petData = FileSystem.ReadAllLines(path)
                        .Select(x => x.Split('='))
                        .Where(x => x.Length == 2)
                        .ToDictionary(x => x[0], x => x[1]);
                    output.Add(new Tuple<int, Pet>(int.Parse(entry.Key), CreatePetFromFile(petData, rarityMap)));
                }
                return output;
            }
            catch
            {
                return new List<Tuple<int, Pet>>();
            }
        }

        private static Pet CreatePetFromFile(Dictionary<string, string> fileData, Dictionary<string, int> rarities)
        {
            return new Pet()
            {
                Name = fileData["Name"],
                Description = fileData["Description"],
                RarityId = rarities[fileData["Rarity"]],
            };
        }

        public static Dictionary<int, int> ImportPetDataIntoSql(string contentFolderName, string petDataPath, string petFolder, IRepository<Pet> petRepository, IRepository<PetRarity> rarityRepository)
        {
            var rarityMap = SeedPetRarity(rarityRepository);

            var pets = LoadPetData(contentFolderName, petDataPath, petFolder, rarityMap);
            foreach (var pet in pets.OrderBy(x => x.Item1))
            {
                petRepository.Create(pet.Item2);
            }
            petRepository.Commit();
            return pets.ToDictionary(x => x.Item1, x => x.Item2.Id);
        }
    }
}
