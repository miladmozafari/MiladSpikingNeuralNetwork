﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSNN.LearningRules
{
	[Serializable]
	public class STDPLearningRule : ILearningRule
	{
		//STDP Parameters
		private int taoPositive, taoNegative;
		private float ampPositive, ampNegative;
		string preNeuronStatisticsKey;
		string postNeuronStatisticsKey;
		float dopamineConstant;
		bool isDopaminergic;
		float dopamineC;

		/// <summary>
		/// Gets whether the rule requires pre neuron statistics or not
		/// </summary>
		public bool HasPreNeuronStatistics
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Gets whether the rule requires post neuron statistics or not
		/// </summary>
		public bool HasPostNeuronStatistics
		{
			get
			{
				return true;
			}
		}

		public bool IsDopaminergic
		{
			get { return isDopaminergic; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stdpAmpPositive">STDP positive change amplify factor</param>
		/// <param name="stdpAmpNegative">STDP negative change amplify factor</param>
		/// <param name="stdpTaoPositive">STDP positive window</param>
		/// <param name="stdpTaoNegative">STDP negative window</param>
		/// <param name="neighborSpikeCount">Number of last learning interval (pre/post) spikes to be 
		/// stored for the new interval. 0 (default value) leads to including all of the spikes in the learning interval.</param>
		/// <param name="synapseDelay">Delay of the synapse.</param> 
		/// <param name="dopamineConstant">Tc in dopamine influence factor. -1 (default value) cancels the influence of 
		/// dopamine.</param>
		public STDPLearningRule(float stdpAmpPositive, float stdpAmpNegative, int stdpTaoPositive, int stdpTaoNegative,
			float dopamineConstant = -1)
		{
			ampPositive = stdpAmpPositive;
			ampNegative = stdpAmpNegative;
			taoPositive = stdpTaoPositive;
			taoNegative = stdpTaoNegative;

			preNeuronStatisticsKey = string.Format("STDP_{0}", taoPositive);
			postNeuronStatisticsKey = string.Format("STDP_{0}", taoNegative);

			this.dopamineConstant = dopamineConstant;
			dopamineC = 0;
			isDopaminergic = (dopamineConstant != -1);
		}

		public float GetWeightChange(Synapse synapse)
		{
			List<int> lastPreSpikes = (List<int>)synapse.PreNeuron.GetStatistics(preNeuronStatisticsKey).GetData()[0];
			List<int> lastPostSpikes = (List<int>)synapse.PostNeuron.GetStatistics(postNeuronStatisticsKey).GetData()[0];
			if(isDopaminergic)
				return GetWeightChangeWithDopamine(synapse, lastPreSpikes, lastPostSpikes);
			return GetWeightChange(synapse, lastPreSpikes, lastPostSpikes);
		}

		public float GetWeightChange(Synapse synapse, List<int> lastPreSpikes, List<int> lastPostSpikes)
		{
			if (Network.Time == 0)
				return 0;

			float dw = 0;
			//STDP based on nearest firing
			STDPTimeContainer preSpkTimes = new STDPTimeContainer(lastPreSpikes, synapse.PreNeuron.SpikeTimes,
				synapse.Delay);
			STDPTimeContainer postSpkTimes = new STDPTimeContainer(lastPostSpikes, synapse.PostNeuron.SpikeTimes);

			int t = Network.Time - Network.LearningInterval;
			int idxPre = 0;
			int idxPost = 0;

			for (; t < Network.Time; t++)
			{
				if (idxPre != preSpkTimes.Count && t == preSpkTimes[idxPre])
				{
					if (idxPost >= 0 && idxPost < postSpkTimes.Count && t > postSpkTimes[idxPost])
						dw += STDP(postSpkTimes[idxPost] - t);
					else if (idxPost - 1 >= 0 && t > postSpkTimes[idxPost - 1])
						dw += STDP(postSpkTimes[idxPost - 1] - t);
				}
				if (idxPost != postSpkTimes.Count && t == postSpkTimes[idxPost])
				{
					if (idxPre >= 0 && idxPre < preSpkTimes.Count && t >= preSpkTimes[idxPre])
						dw += STDP(t - preSpkTimes[idxPre]);
					else if (idxPre - 1 >= 0 && t >= preSpkTimes[idxPre - 1])
						dw += STDP(t - preSpkTimes[idxPre - 1]);
				}

				//going forward
				if (idxPre != preSpkTimes.Count && t == preSpkTimes[idxPre])
					idxPre++;
				if (idxPost != postSpkTimes.Count && t == postSpkTimes[idxPost])
					idxPost++;
			}
			
			return dw;
		}

		public float GetWeightChangeWithDopamine(Synapse synapse, List<int> lastPreSpikes, List<int> lastPostSpikes)
		{
			if (Network.Time == 0)
				return 0;

			//STDP based on nearest firing
			STDPTimeContainer preSpkTimes = new STDPTimeContainer(lastPreSpikes, synapse.PreNeuron.SpikeTimes,
				synapse.Delay);
			STDPTimeContainer postSpkTimes = new STDPTimeContainer(lastPostSpikes, synapse.PostNeuron.SpikeTimes);

			int t = Network.Time - Network.LearningInterval;
			int idxPre = 0;
			int idxPost = 0;

			for (; t < Network.Time; t++)
			{
				if (idxPre != preSpkTimes.Count && t == preSpkTimes[idxPre])
				{
					if (idxPost >= 0 && idxPost < postSpkTimes.Count && t > postSpkTimes[idxPost])
						dopamineC += STDP(postSpkTimes[idxPost] - t);
					else if (idxPost - 1 >= 0 && t > postSpkTimes[idxPost - 1])
						dopamineC += STDP(postSpkTimes[idxPost - 1] - t);
				}
				if (idxPost != postSpkTimes.Count && t == postSpkTimes[idxPost])
				{
					if (idxPre >= 0 && idxPre < preSpkTimes.Count && t >= preSpkTimes[idxPre])
						dopamineC += STDP(t - preSpkTimes[idxPre]);
					else if (idxPre - 1 >= 0 && t >= preSpkTimes[idxPre - 1])
						dopamineC += STDP(t - preSpkTimes[idxPre - 1]);
				}

				//going forward
				if (idxPre != preSpkTimes.Count && t == preSpkTimes[idxPre])
					idxPre++;
				if (idxPost != postSpkTimes.Count && t == postSpkTimes[idxPost])
					idxPost++;
			}

			float dw = dopamineC * Dopamine.GetDopamineLevel(t - 1);
			dopamineC -= dopamineC / dopamineConstant;
			return dw;
		}

		private float STDP(int postPreSpikeTimeDifference)
		{
			if (postPreSpikeTimeDifference >= 0)
			{
				if (postPreSpikeTimeDifference >= taoPositive)
					return 0;
				return ampPositive * (float)Math.Exp(-postPreSpikeTimeDifference / (float)taoPositive);
			}
			if (-1 * postPreSpikeTimeDifference >= taoNegative)
				return 0;
			return ampNegative * (float)Math.Exp(postPreSpikeTimeDifference / (float)taoNegative);
		}

		public ILearningRule GetCopy()
		{
			STDPLearningRule copy = new STDPLearningRule(this.ampPositive, this.ampNegative,
				this.taoPositive, this.taoNegative, this.dopamineConstant);
			return copy;
		}

		public NeuronStatistics GetPreNeuronStatistics(Neuron neuron)
		{
			return new STDPNeuronStatistics(neuron, taoPositive);
		}

		public string GetPreNeuronStatisticsKey()
		{
			return preNeuronStatisticsKey;
		}

		public NeuronStatistics GetPostNeuronStatistics(Neuron neuron)
		{
			return new STDPNeuronStatistics(neuron, taoNegative);
		}

		public string GetPostNeuronStatisticsKey()
		{
			return postNeuronStatisticsKey;
		}
	}
}
