﻿using System;
using Pisces.Domain.Models.Alleles;
using Pisces.Domain.Types;

namespace Pisces.Domain.Utility
{
    public static class AlleleHelper
    {
        public static AlleleType GetAlleleType(string alleleString)
        {
            return GetAlleleType(Convert.ToChar(alleleString));
        }
        public static AlleleType GetAlleleType(char alleleChar)
        {
            switch (alleleChar)
            {
                case 'A':
                    return AlleleType.A;
                case 'C':
                    return AlleleType.C;
                case 'G':
                    return AlleleType.G;
                case 'T':
                    return AlleleType.T;
                case 'N':
                    return AlleleType.N;
                default:
                    //be kinder to unknown bases.
                    return AlleleType.N;
                    //throw new ArgumentException(string.Format("Unrecognized allele '{0}'.", alleleChar));
            }
        }

        public static CandidateAllele Map(CalledAllele called)
        {
            var candidateAllele = new CandidateAllele(called.Chromosome, called.Coordinate, called.Reference,
                called.Alternate, called.Type);

            Array.Copy(called.SupportByDirection, candidateAllele.SupportByDirection, called.SupportByDirection.Length);

            if (called.Type != AlleleCategory.Reference)
            {
                for (var i = 0; i < called.ReadCollapsedCounts.Length; i++)
                    candidateAllele.ReadCollapsedCounts[i] = called.ReadCollapsedCounts[i];
            }

            return candidateAllele;
        }

        public static CalledAllele Map(CandidateAllele candidate)
        {
            /*
            var calledAllele = candidate.Type == AlleleCategory.Reference
                ? (BaseCalledAllele)new BaseCalledAllele()
                : new BaseCalledAllele(candidate.Type);
                */

            var calledAllele = new CalledAllele(candidate.Type);

            calledAllele.Alternate = candidate.Alternate;
            calledAllele.Reference = candidate.Reference;
            calledAllele.Chromosome = candidate.Chromosome;
            calledAllele.Coordinate = candidate.Coordinate;
            calledAllele.AlleleSupport = candidate.Support;
            Array.Copy(candidate.SupportByDirection, calledAllele.SupportByDirection, candidate.SupportByDirection.Length);

            if (candidate.Type != AlleleCategory.Reference)
            {
                for (var i = 0; i < candidate.ReadCollapsedCounts.Length; i++)
                    calledAllele.ReadCollapsedCounts[i] = candidate.ReadCollapsedCounts[i];
            }
            return calledAllele;
        }
    }
}
