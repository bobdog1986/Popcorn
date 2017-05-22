using System.Collections.Generic;
using Popcorn.Models.Movie;

namespace Popcorn.Comparers
{
    /// <summary>
    /// Compare two movies
    /// </summary>
    public class MovieComparer : IEqualityComparer<MovieJson>
    {
        /// <summary>
        /// Compare two movies
        /// </summary>
        /// <param name="x">First movie</param>
        /// <param name="y">Second movie</param>
        /// <returns>True if both movies are the same, false otherwise</returns>
        public bool Equals(MovieJson x, MovieJson y)
        {
            //Check whether the compared objects reference the same data.
            if (ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;

            return x.ImdbCode == y.ImdbCode && x.DateUploadedUnix == y.DateUploadedUnix;
        }

        /// <summary>
        /// Define a unique hash code for a movie
        /// </summary>
        /// <param name="movie">A movie</param>
        /// <returns>Unique hashcode</returns>
        public int GetHashCode(MovieJson movie)
        {
            //Check whether the object is null
            if (ReferenceEquals(movie, null)) return 0;

            //Get hash code for the Id field
            var hashId = movie.ImdbCode.GetHashCode();

            //Get hash code for the Date field.
            var hashDate = movie.DateUploadedUnix.GetHashCode();

            return hashId ^ hashDate;
        }
    }
}