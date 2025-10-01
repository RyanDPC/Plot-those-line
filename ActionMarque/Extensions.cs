using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ActionMarque
{
    /// <summary>
    /// Méthodes d'extension pour améliorer la lisibilité du code
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Extension pour appliquer une action sur chaque élément
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
        /// Extension pour appliquer une action et retourner l'objet
        /// </summary>
        public static T Let<T>(this T obj, Action<T> action) where T : class
        {
            if (obj != null)
                action(obj);
            return obj;
        }

        /// <summary>
        /// Extension pour observer une valeur (utile pour debug)
        /// </summary>
        public static T Tap<T>(this T obj, Action<T> action)
        {
            action(obj);
            return obj;
        }

        /// <summary>
        /// Extension pour définir position et taille
        /// </summary>
        public static T AtPosition<T>(this T control, int x, int y, int width, int height) where T : Control
        {
            control.Location = new Point(x, y);
            control.Size = new Size(width, height);
            return control;
        }

        /// <summary>
        /// Extension pour ajouter un effet hover
        /// </summary>
        public static Panel WithHoverEffect(this Panel panel, Color normalColor, Color hoverColor)
        {
            panel.BackColor = normalColor;
            panel.MouseEnter += (s, e) => panel.BackColor = hoverColor;
            panel.MouseLeave += (s, e) => panel.BackColor = normalColor;
            return panel;
        }

        /// <summary>
        /// Extension pour configurer un label moderne
        /// </summary>
        public static Label WithModernStyle(this Label label, Color foreColor, int fontSize = 9, FontStyle style = FontStyle.Regular)
        {
            label.ForeColor = foreColor;
            label.Font = new Font("Segoe UI", fontSize, style);
            label.BackColor = Color.Transparent;
            return label;
        }

        /// <summary>
        /// Extension pour ajouter des contrôles
        /// </summary>
        public static Panel AddControls(this Panel panel, params Control[] controls)
        {
            panel.Controls.AddRange(controls);
            return panel;
        }

        /// <summary>
        /// Extension : alias de Where
        /// </summary>
        public static IEnumerable<T> Filter<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            return source.Where(predicate);
        }

        /// <summary>
        /// Extension pour exécuter une action finale
        /// </summary>
        public static IEnumerable<T> Finally<T>(this IEnumerable<T> source, Action action)
        {
            var list = source.ToList();
            action();
            return list;
        }
    }
}
