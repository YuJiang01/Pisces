﻿using System.Collections.Generic;
using System.Linq;
using VariantPhasing.Logic;
using VariantPhasing.Models;
using Xunit;

namespace VariantPhasing.Tests.Logic
{
    public class VariantPhaserTests
    {
        [Fact]
        public void GetPhasingProbabilities()
        {
            var variantSites = new List<VariantSite>
            {
                new VariantSite(1),
                new VariantSite(2),
                new VariantSite(45)
            };

            var clusters = new SetOfClusters(new ClusteringParameters());

            // There should be a PhasingResult for each variant in variantSites
            var phasingProbabilities = VariantPhaser.GetPhasingProbabilities(variantSites, clusters);
            Assert.Equal(variantSites.Count, phasingProbabilities.Count);
            Assert.Equal(variantSites.Select(x=>x), phasingProbabilities.Keys.ToList());

        }
    }
}
