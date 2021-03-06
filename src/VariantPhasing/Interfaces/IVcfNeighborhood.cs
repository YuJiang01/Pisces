﻿using System.Collections.Generic;
using Pisces.Domain.Models.Alleles;

namespace VariantPhasing.Interfaces
{
    public interface IVcfNeighborhood
    {
        List<CalledAllele> CandidateVariants { get; }
        Dictionary<int, List<CalledAllele>> CalledVariants { get; set; }
        Dictionary<int, CalledAllele> CalledRefs { get; set; }
        List<CalledAllele> Refs { get; }
        Dictionary<int, int> UsedRefCountsLookup { get; }
        List<CalledAllele> GetOriginalVcfVariants();
        int LastPositionOfInterestWithLookAhead { get; }
        int LastPositionOfInterestInVcf { get; }
        int FirstPositionOfInterest { get; }
        string ReferenceName { get; }
    }
}