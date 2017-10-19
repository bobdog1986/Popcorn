using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.Services.Associations
{
    public interface IFileAssociationService
    {
        void RegisterTorrentFileAssociation();
        void RegisterMagnetLinkAssociation();
        void UnregisterMagnetLinkAssociation();
        void UnregisterTorrentFileAssociation();
        bool TorrentFileAssociationIsEnabled();
        bool MagneLinkAssociationIsEnabled();
    }
}
