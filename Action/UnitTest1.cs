using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Action
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestActionMarque_AjoutMarque()
        {
            // Arrange / Given
            var listeMarques = new List<MarqueItem>();
            var nouvelleMarque = new MarqueItem 
            { 
                Nom = "Apple", 
                Valeurs = new double[] { 150.0, 155.0, 160.0, 165.0, 170.0 },
                IsVisible = true,
                Status = "positive"
            };
            
            // Act / When
            // Ajouter une nouvelle marque à la liste
            listeMarques.Add(nouvelleMarque);
            
            // Assert / Then
            // La marque devrait être ajoutée à la liste
            Assert.AreEqual(1, listeMarques.Count, "La liste devrait contenir 1 marque");
            Assert.AreEqual("Apple", listeMarques[0].Nom, "Le nom de la marque devrait être Apple");
            Assert.AreEqual(5, listeMarques[0].Valeurs.Length, "La marque devrait avoir 5 valeurs");
            Assert.AreEqual(150.0, listeMarques[0].Valeurs[0], "La première valeur devrait être 150.0");
            Assert.AreEqual(170.0, listeMarques[0].Valeurs[4], "La dernière valeur devrait être 170.0");
            Assert.IsTrue(listeMarques[0].IsVisible, "La marque devrait être visible");
            Assert.AreEqual("positive", listeMarques[0].Status, "Le statut devrait être positive");
        }

        [TestMethod]
        public void TestActionMarque_SuppressionMarque()
        {
            // Arrange / Given
            var listeMarques = new List<MarqueItem>
            {
                new MarqueItem { Nom = "Apple", Valeurs = new double[] { 150.0, 155.0, 160.0 } },
                new MarqueItem { Nom = "Tesla", Valeurs = new double[] { 800.0, 780.0, 760.0 } },
                new MarqueItem { Nom = "Microsoft", Valeurs = new double[] { 300.0, 305.0, 310.0 } }
            };
            string nomMarqueASupprimer = "Tesla";
            
            // Act / When
            // Supprimer la marque Tesla de la liste
            var marqueASupprimer = listeMarques.FirstOrDefault(m => m.Nom == nomMarqueASupprimer);
            bool resultat = marqueASupprimer != null;
            if (resultat)
            {
                listeMarques.Remove(marqueASupprimer);
            }
            
            // Assert / Then
            // La marque devrait être supprimée de la liste
            Assert.IsTrue(resultat, "La suppression devrait réussir");
            Assert.AreEqual(2, listeMarques.Count, "La liste devrait contenir 2 marques après suppression");
            Assert.IsFalse(listeMarques.Any(m => m.Nom == "Tesla"), "Tesla ne devrait plus être dans la liste");
            Assert.IsTrue(listeMarques.Any(m => m.Nom == "Apple"), "Apple devrait toujours être dans la liste");
            Assert.IsTrue(listeMarques.Any(m => m.Nom == "Microsoft"), "Microsoft devrait toujours être dans la liste");
        }

        [TestMethod]
        public void TestActionMarque_CalculStatistiquesMinMaxAvg()
        {
            // Arrange / Given
            var valeursPrix = new double[] { 100.0, 120.0, 95.0, 140.0, 110.0, 130.0, 105.0, 135.0 };
            string nomMarque = "Apple";
            
            // Act / When
            // Calculer les statistiques min, max et moyenne
            double min = valeursPrix.Min();
            double max = valeursPrix.Max();
            double moyenne = valeursPrix.Average();
            int nombreValeurs = valeursPrix.Length;
            
            // Assert / Then
            // Les statistiques devraient être calculées correctement
            Assert.AreEqual(95.0, min, 0.01, "Le minimum devrait être 95.0");
            Assert.AreEqual(140.0, max, 0.01, "Le maximum devrait être 140.0");
            Assert.AreEqual(116.875, moyenne, 0.01, "La moyenne devrait être 116.875");
            Assert.AreEqual(8, nombreValeurs, "Le nombre de valeurs devrait être 8");
            
            // Vérifier que min <= moyenne <= max
            Assert.IsTrue(min <= moyenne, "Le minimum devrait être <= à la moyenne");
            Assert.IsTrue(moyenne <= max, "La moyenne devrait être <= au maximum");
        }

    }

    // Classe pour représenter une marque avec ses données
    public class MarqueItem
    {
        public string Nom { get; set; }
        public double[] Valeurs { get; set; }
        public bool IsVisible { get; set; } = true;
        public string Status { get; set; } = "neutral";
    }
}
