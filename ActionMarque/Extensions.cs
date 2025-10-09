using System;
using System.Collections.Generic;
using System.Linq;

namespace ActionMarque
{
    /// <summary>
    /// Méthodes d'extension pour améliorer la lisibilité du code
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Extension pour appliquer une action sur chaque élément
        /// Utilisée pour les itérations avec effet de bord
        /// </summary>
        public static IEnumerable<T> ForEachDo<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
            return source;
        }

        /// <summary>
        /// Extension pour filtrer une collection (alias de Where)
        /// Utilisée pour le filtrage de données dans l'application
        /// </summary>
        public static IEnumerable<T> Filter<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            return source.Where(predicate);
        }
    }
}
