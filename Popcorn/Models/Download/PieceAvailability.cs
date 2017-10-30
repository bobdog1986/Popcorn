using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.Models.Download
{
    public class PieceAvailability
    {
        public readonly int TotalPieces;
        public readonly int StartAvailablePiece;
        public readonly int EndAvailablePiece;

        public PieceAvailability(int totalPieces, int startAvailablePiece, int endAvailablePiece)
        {
            TotalPieces = totalPieces;
            StartAvailablePiece = startAvailablePiece;
            EndAvailablePiece = endAvailablePiece;
        }
    }
}
