﻿using System;
using System.Collections.Generic;
using System.Linq;
using Pisces.Domain;
using Pisces.Domain.Models;
using Pisces.Domain.Models.Alleles;
using Pisces.Domain.Types;
using Pisces.Domain.Utility;

namespace Pisces.Processing.RegionState
{
    public class RegionState : Region
    {
        private List<CandidateAllele>[] _candidateVariantsLookup;
        private int[,,] _alleleCounts;
		private double[,,] _sumOfAlleleBaseQualities;
		private int[] _gappedMnvReferenceCounts;
        private List<ReadCoverageSummary>[] _coverageSummaries;
 
        public int MaxAlleleEndpoint { get; private set; }

        public string Name
        {
            get { return ToString(); }
        }

        /// <summary>
        /// Genome region is inclusive of both start and end positions.
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="endPosition"></param>
        public RegionState(int startPosition, int endPosition) : base (startPosition, endPosition)
        {
            Initialize();
        }

        /// <summary>
        /// Validates start and end positions.  Initializes internal state arrays by either creating new arrays if starting from scratch or region size has changed length, or
        /// clearing out the state of existing arrays.  Note, for this application, we always have fixed region sizes.
        /// </summary>
        private void Initialize()
        {
            var regionSize = EndPosition - StartPosition + 1;

            _alleleCounts = new int[regionSize, Constants.NumAlleleTypes, Constants.NumDirectionTypes];
            _gappedMnvReferenceCounts = new int[regionSize];
            _candidateVariantsLookup = new List<CandidateAllele>[regionSize];
            _coverageSummaries = new List<ReadCoverageSummary>[regionSize];
			_sumOfAlleleBaseQualities = new double[regionSize, Constants.NumAlleleTypes, Constants.NumDirectionTypes];

		}

        /// <summary>
        /// Reset object to new region
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="endPosition"></param>
        public void Reset(int startPosition, int endPosition)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;

            Initialize();
        }


		/// <summary>
		/// Add ref count taken up by gapped mnv. 
		/// </summary>
		/// <param name="position"></param>
		/// <param name="count"></param>
		public void AddGappedMnvRefCount(int position, int count)
        {
            if (IsPositionInRegion(position))
            {
                _gappedMnvReferenceCounts[position - StartPosition] += count;
            }
        }

        public void AddCandidate(CandidateAllele candidate, bool trackOpenEnded = false)
        {
            if (candidate.Type == AlleleCategory.Reference) throw new ArgumentException(string.Format("Unable to add candidate '{0}': reference candidates are not tracked.", candidate));

            if (!IsPositionInRegion(candidate.Coordinate))
                throw new ArgumentException(string.Format("Unable to add candidate at position {0} to region '{1}'",
                    candidate.Coordinate, Name));

            var regionIndex = candidate.Coordinate - StartPosition;
            var existingCandidates = _candidateVariantsLookup[regionIndex];

            if (existingCandidates == null)
                _candidateVariantsLookup[regionIndex] = new List<CandidateAllele> { candidate };
            else
            {
                //TJD - this used to be a hash table, not a find,
                //where each variants unique signature was the key. 
                //this might be why we have seen a performance hit in the new pisces.

                var foundAtIndex = trackOpenEnded ?                   
                    existingCandidates.FindIndex(c => c.Equals(candidate)
                                                 && c.OpenOnLeft == candidate.OpenOnLeft 
                                                 && c.OpenOnRight == candidate.OpenOnRight) :
                    existingCandidates.FindIndex(c => c.Equals(candidate));

                if (foundAtIndex == -1)
                {
                    existingCandidates.Add(candidate);
                }
                else
                {
                    var existingMatch = existingCandidates[foundAtIndex];

                    for (var i = 0; i < existingMatch.SupportByDirection.Length; i++)
                        existingMatch.SupportByDirection[i] += candidate.SupportByDirection[i];

                    for (var i = 0; i < existingMatch.ReadCollapsedCounts.Length; i++)
                        existingMatch.ReadCollapsedCounts[i] += candidate.ReadCollapsedCounts[i];
                }
            }

            UpdateMaxPosition(candidate);
        }

        private void UpdateMaxPosition(CandidateAllele candidate)
        {
            int otherEnd = 0;
            switch (candidate.Type)
            {
                case AlleleCategory.Deletion:
                    otherEnd = candidate.Coordinate + candidate.Reference.Length;
                    break;
                case AlleleCategory.Insertion:
                    otherEnd = candidate.Coordinate + 1;
                    break;
                case AlleleCategory.Mnv:
                    otherEnd = candidate.Coordinate + candidate.Reference.Length - 1;
                    break;
            }
            if (otherEnd > MaxAlleleEndpoint)
            {
                MaxAlleleEndpoint = otherEnd;
            }            
        }

        public void AddAlleleCount(int position, AlleleType alleleType, DirectionType directionType)
        {
            if (IsPositionInRegion(position))
            {
                _alleleCounts[position - StartPosition, (int) alleleType, (int) directionType]++;
            }
        }

		public void AddBaseQualites(int position, AlleleType alleleType, DirectionType directionType, double baseQuality)
		{
			if (IsPositionInRegion(position))
			{
				_sumOfAlleleBaseQualities[position - StartPosition, (int)alleleType, (int)directionType] += baseQuality;
			}
		}
		public void AddReadSummary(int position, ReadCoverageSummary summary)
        {
            if (IsPositionInRegion(position))
            {
                var list = _coverageSummaries[position - StartPosition];
                if (list == null)
                {
                    _coverageSummaries[position - StartPosition] = new List<ReadCoverageSummary> { summary };
                }
                else
                {
                    list.Add(summary);
                }
            }
        }

        private bool IsPositionInRegion(int position)
        {
            return position >= StartPosition && position <= EndPosition;
        }

        public int GetAlleleCount(int position, AlleleType alleleType, DirectionType directionType)
        {
			if (!IsPositionInRegion(position))
				throw new ArgumentException(string.Format("Position {0} is not in region '{1}'.", position, Name));

			return _alleleCounts[position - StartPosition, (int)alleleType, (int)directionType];
		}


		public double GetSumOfAlleleBaseQualites(int position, AlleleType alleleType, DirectionType directionType)
		{
			if (!IsPositionInRegion(position))
				throw new ArgumentException(string.Format("Position {0} is not in region '{1}'.", position, Name));

			return _sumOfAlleleBaseQualities[position - StartPosition, (int)alleleType, (int)directionType];
		}
		public List<ReadCoverageSummary> GetReadSummaries(int position)
        {
            if (!IsPositionInRegion(position))
                throw new ArgumentException(string.Format("Position {0} is not in region '{1}'.", position, Name));

            return _coverageSummaries[position - StartPosition];
        }

        public int GetGappedMnvRefCount(int position)
        {
            if (!IsPositionInRegion(position))
                throw new ArgumentException(string.Format("Position {0} is not in region '{1}'.", position, Name));

            return _gappedMnvReferenceCounts[position - StartPosition];
        }

        public List<CandidateAllele> GetAllCandidates(bool includeRefAlleles, ChrReference chrReference,
            ChrIntervalSet intervals = null)
        {
            var alleles = new List<CandidateAllele>();

            // add all candidates - these are potentially collapsable targets
            foreach(var positionLookup in _candidateVariantsLookup)
                if (positionLookup != null)
                    alleles.AddRange(positionLookup);

            if (includeRefAlleles)
            {
                var regionsToFetch = intervals == null
                    ? new List<Region> {this} // fetch whole block region
                    : intervals.GetClipped(this); // clip intervals to block region

                for (var i = 0; i < regionsToFetch.Count; i ++)
                {
                    var clippedInterval = regionsToFetch[i];
                    for (var position = clippedInterval.StartPosition;
                        position <= clippedInterval.EndPosition;
                        position ++)
                    {
                        var positionIndex = position - StartPosition;

                        // add ref alleles within region to fetch - note that zero coverage ref positions are only added if input intervals provided
                        if (position > chrReference.Sequence.Length)
                            break;

                        var refBase = chrReference.Sequence[position - 1].ToString();

                        var refBaseIndex = (int) AlleleHelper.GetAlleleType(refBase);
                        var refAllele = new CandidateAllele(chrReference.Name, position,
                            refBase, refBase, AlleleCategory.Reference);

                        // gather support for allele
                        var totalSupport = 0;

                        for (var alleleTypeIndex = 0; alleleTypeIndex < Constants.NumAlleleTypes; alleleTypeIndex++)
                        {
                            for (var directionIndex = 0; directionIndex < Constants.NumDirectionTypes; directionIndex++)
                            {
                                var count = _alleleCounts[positionIndex, alleleTypeIndex, directionIndex];
                                if (alleleTypeIndex == refBaseIndex)
                                    refAllele.SupportByDirection[directionIndex] = count;

                                totalSupport += count;
                            }
                        }

                        if (intervals != null || totalSupport > 0)
                            alleles.Add(refAllele);
                    }
                }
            }

            return alleles;
        }

        public List<CandidateAllele> ExtractCollapsable(int upToPosition)
        {
            var allCollapsable = new List<CandidateAllele>();

            foreach (var lookup in _candidateVariantsLookup)
            {
                if (lookup == null) continue;

                var collapsables = lookup.Where(c =>
                        c.Coordinate + c.Alternate.Length - 1 <= upToPosition &&
                        !c.OpenOnRight && 
                        (c.Type == AlleleCategory.Mnv || c.Type == AlleleCategory.Snv)).ToList();

                allCollapsable.AddRange(collapsables);

                foreach (var collapsable in collapsables)
                    lookup.Remove(collapsable);
            }

            return allCollapsable;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RegionState)) return false;

            var otherRegion = (RegionState) obj;

            return otherRegion.StartPosition == StartPosition &&
                   otherRegion.EndPosition == EndPosition;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
