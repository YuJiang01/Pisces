﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Pisces.IO.Sequencing;
using Pisces.IO;

namespace VariantQualityRecalibration
{
    public class MutationCounter 
    {
        // idea is to keep track of the disparity between two pools as a measure of FFPE degradation,
        // or overall oxidation affecting tissue sample.


        //possible SNP changes:
        //
        //
        // *    A   C   G   T
        //  A   *   1   2   3
        //  C   4   *   5   6
        //  G   7   8   *   9
        //  T   10  11  12  *
        //

        private StreamWriter _writer;
        private double _totalPossibleMutations = 0;
        private Dictionary<MutationCategory, double> _mutationCount = new Dictionary<MutationCategory, double>();


        public double TotalMutations
        {
            get
            {
                return _mutationCount.Values.Sum();             
            }

        }


        public double ObservedMutationRate
        {
            get
            {
                if (_totalPossibleMutations == 0)
                    return 0;

               return (TotalMutations / _totalPossibleMutations);
            }
        }

        public MutationCounter()
        {
            var values = GetAllMutationCategories();

            foreach (MutationCategory mutation in values)
            {
                _mutationCount.Add(mutation,0);
            }

        }

        public static List<MutationCategory> GetAllMutationCategories()
        {
            var Categories =
                Enum.GetValues(typeof(MutationCategory)).OfType<MutationCategory>().ToList();
            
            return Categories;
        }

        public void StartWriter(string outFile)
        {
            _writer = new StreamWriter(outFile);
        }

        public void CloseFalseCallsWriter()
        {
            if (_writer != null)
            {

                _writer.WriteLine();
                _writer.WriteLine("CountsByCategory");
                foreach (MutationCategory mutation in _mutationCount.Keys)
                {
                    _writer.WriteLine(mutation + "\t" + _mutationCount[mutation]);
                }

                _writer.WriteLine();
                _writer.WriteLine("AllPossibleVariants\t" + _totalPossibleMutations);
                _writer.WriteLine("VariantsCountedTowardEstimate\t" + TotalMutations);
                _writer.WriteLine("MismatchEstimate(%)\t{0:N4}", (ObservedMutationRate * 100));
                _writer.Close();
                _writer.Dispose();
            }
        }

        public bool Add(VcfVariant variant)
        {
            if (variant == null)
                return false;

            var category = GetMutationCategory(variant);
            _totalPossibleMutations++;

            if (category != MutationCategory.Reference)
            {
                //then this variant has never been found, and we decided it is not going to count for our buckets.
                _mutationCount[category]++;

                return true;
            }


            return false;
        }

     

        public static MutationCategory GetMutationCategory(string EnumString)
        {
            return (MutationCategory)Enum.Parse(typeof(MutationCategory), EnumString);
        }

        public static MutationCategory GetMutationCategory(
            VcfVariant consensusVariant)
        {

            if (consensusVariant.VariantAlleles.Length == 0)
                return MutationCategory.Reference;

            if (consensusVariant.VariantAlleles.Length > 1)
            {
                throw new ApplicationException("This method is expecting only one variant allele per variant entry");
            }

            int refLength = consensusVariant.ReferenceAllele.Length;
            int altLength = consensusVariant.VariantAlleles[0].Length;

            if (refLength > altLength)
                return MutationCategory.Deletion;

            if (refLength < altLength)
                return MutationCategory.Insertion;

            if ((refLength != 1) || (altLength != 1))
                return MutationCategory.Other;

            if ((consensusVariant.VariantAlleles[0] == ".")
                || (consensusVariant.VariantAlleles[0] == consensusVariant.ReferenceAllele))
                return MutationCategory.Reference;

            var EnumString = consensusVariant.ReferenceAllele + "to" + consensusVariant.VariantAlleles[0];
       
            foreach (MutationCategory mutation in GetAllMutationCategories())
            {
                if (EnumString.ToLower() == mutation.ToString().ToLower())
                    return mutation;
            }
            
            return MutationCategory.Other;
        }

    }
}
