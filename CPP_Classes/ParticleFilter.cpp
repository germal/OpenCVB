#include "ParticleFilter.h"

#define CV_REDUCE_SUM 0
#define CV_REDUCE_AVG 1
#define CV_REDUCE_MAX 2
#define CV_REDUCE_MIN 3
ParticleFilter::ParticleFilter(void)
{

}

ParticleFilter::ParticleFilter(const Mat &inZ, const Mat &inR, unsigned int inN)
{
	// TODO: Improve by asserting using opencv assert calls
	if(inZ.empty() || inR.empty())
	{
		std::cout << "Error: insufficient inputs" << std::endl;
	}

	this->N = inN;
	this->R = inR.clone();

	this->N_threshold = (int)((float)(6*N)/(float)10);

	selectDynamicModel(inZ.rows); //  with dimensions of observation

	this->Ds = this->A.cols; // A is square matrix providing dimensions of state vector

	Mat lR, uR; // store upper and lower bounds for each particle

	// assuming that the state vector has constant velocity model
	lR = Mat::zeros(3*inZ.rows, 1, CV_32FC1);
	uR = Mat::zeros(3*inZ.rows, 1, CV_32FC1);
	
	lR.at<float>(0, 0) = inZ.at<float>(0, 0); // posX
	lR.at<float>(1, 0) = -30;				  // velX
	lR.at<float>(2, 0) = -1;				  // accX

	lR.at<float>(0+3, 0) = inZ.at<float>(1, 0); // posY
	lR.at<float>(1+3, 0) = -30;				  // velY
	lR.at<float>(2+3, 0) = -1;				  // accY

	uR.at<float>(0, 0) = inZ.at<float>(0, 0);
	uR.at<float>(1, 0) = 30;
	uR.at<float>(2, 0) = 1;

	uR.at<float>(0+3, 0) = inZ.at<float>(1, 0);
	uR.at<float>(1+3, 0) = 30;
	uR.at<float>(2+3, 0) = 1;
	
	initParticles(lR, uR);

}

void ParticleFilter::resampleParticles()
{
	Mat outXn = Mat::zeros(Xn.rows, Xn.cols, Xn.type());

	Mat Neff;

	reduce(Wn.mul(Wn), Neff, 1, CV_REDUCE_SUM);

	//std::cout << Neff.cols << " " << Neff.rows << std::endl;

	if(Neff.at<float>(0, 0) < this->N_threshold)
	{
		Mat outIdx = this->resampler(Wn);
		Wn = Mat::ones(Wn.rows, Wn.cols, Wn.type());
		this->normalizeWeights();
		for(int i = 0; i < this->N; i++)
		{
			Xn.col(outIdx.at<float>(0, i)).copyTo(outXn.col(i));
		}

		Xn = outXn.clone();
	}	
}

void ParticleFilter::predict()
{
	Mat gaussianNoise = Mat::zeros(Ds, N, CV_32FC1);
	theRNG().fill(gaussianNoise, RNG::NORMAL, 0, 3);
	//Mat RTemp = repeat(R, 1, N);
	Xn = A*Xn + R*gaussianNoise;
}

void ParticleFilter::update(const Mat &inZ)
{
	weightingParticles(inZ);
}

Mat ParticleFilter::currentPrediction()
{
	Mat pLocs = H * (Xn*Wn.t());;
	return pLocs;
}

Mat ParticleFilter::showParticles(const Mat &inImage)
{
	Vec3b cVal;
	cVal[0] = 0;
	cVal[1] = 0;
	cVal[2] = 255;

	Mat pLocs = H*Xn;

	Mat retImage = inImage.clone();

	for( int i = 0; i < this->N; i++)
	{
		int xIdx = std::floor(pLocs.at<float>(0, i));
		int yIdx = std::floor(pLocs.at<float>(1, i));

		if(xIdx >= 0 && xIdx < retImage.cols && yIdx >= 0 && yIdx < retImage.rows)
			retImage.at<Vec3b>(yIdx, xIdx) = cVal;
	}

	return retImage;
}

Mat ParticleFilter::showPredictedLocation(const Mat &inImage)
{
	Vec3b cVal;
	cVal[0] = 255;
	cVal[1] = 0;
	cVal[2] = 0;

	// find prediction of model
	Mat pLocs = H * (Xn*Wn.t());

	std::cout << "Tracked Location: " << pLocs << std::endl;
	Mat retImage = inImage.clone();

	int xIdx = std::floor(pLocs.at<float>(0, 0));
	int yIdx = std::floor(pLocs.at<float>(1, 0));

	// draw a crosshair
	int sizeIn = 3;
	int sizeOut = 5;
	for(int j = -sizeOut; j < sizeOut+1; j++)
	{
		for(int i = -sizeIn; i < sizeIn+1; i++)
			if(xIdx + i >= 0 && xIdx + i< retImage.cols && yIdx >= 0 && yIdx < retImage.rows)
				retImage.at<Vec3b>(yIdx+j, xIdx + i) = cVal;

		for(int i = -sizeIn; i < sizeIn+1; i++)
			if(xIdx>= 0 && xIdx < retImage.cols && yIdx + i >= 0 && yIdx + i < retImage.rows)
				retImage.at<Vec3b>(yIdx + i, xIdx+j) = cVal;

	}
	return retImage;

}

ParticleFilter::~ParticleFilter(void)
{
	// make sure that large Matrices are cleared
	this->Wn.release();
	this->Xn.release();

	// opencv will automatically clear all variables
}

// Private helper functions

void ParticleFilter::initParticles(Mat &lR, Mat &uR)
{
	Mat Rn = Mat::zeros(this->Ds, this->N, CV_32FC1);
	theRNG().fill(Rn, RNG::UNIFORM, 0, 1);

	Mat Sn = repeat(uR-lR, 1, this->N);

	Mat Tn = repeat(lR, 1, this->N);

	Mat temp = (Rn.mul(Sn) + Tn);

	Xn = temp.clone();
}

// TODO:: Add mode for options for different models
void ParticleFilter::selectDynamicModel(unsigned int D)
{
	A = Mat::zeros(6, 6, CV_32FC1);
	H = Mat::zeros(2, 6, CV_32FC1);

	A.at<float>(0, 0) = 1;
	A.at<float>(0, 1) = 1;
	A.at<float>(1, 1) = 1;
	A.at<float>(1, 2) = 1;

	A.at<float>(0+3, 0+3) = 1;
	A.at<float>(0+3, 1+3) = 1;
	A.at<float>(1+3, 1+3) = 1;
	A.at<float>(1+3, 2+3) = 1;

	H.at<float>(0, 0) = 1;
	H.at<float>(1, 3) = 1;

	std::cout << "Dynamic Model A = " << A << std::endl;
	std::cout << "Observation Model H = " << H << std::endl;
}

void ParticleFilter::normalizeWeights()
{
	Mat wSum;
	reduce(Wn, wSum, 1, CV_REDUCE_SUM);

	Wn = Wn/wSum.at<float>(0, 0);
}

void ParticleFilter::weightingParticles(const Mat &inZ)
{
	Wn = Mat::zeros(1, N, CV_32FC1);
	
	Mat allpZt = H*Xn;
	//std::cout << "inZ" << inZ << std::endl;
	//std::cout << "allpZt" << allpZt << std::endl;
	//Mat pZt;
	
	for( int i = 0; i < N; i++)
	{

		double d = distanceGaussian(inZ, allpZt.col(i).clone());
		//std::cout << "inZ" << inZ << std::endl;
		//std::cout << "allpZt" << allpZt.col(i) << std::endl;

		Wn.at<float>(0, i) = d;

		//std::cout << "Distance is:" << d << std::endl;
	}

	this->normalizeWeights();
}

double ParticleFilter::distanceGaussian(const Mat &inZ, const Mat &pZt)
{
	double sigma = 5;

	int n = inZ.rows;

	double bTerm = 0;

	for(int j = 0; j < n; j++)
	{
		bTerm = bTerm - std::pow(inZ.at<float>(j, 0)-pZt.at<float>(j, 0), 2)/(2*std::pow(sigma, 2));
	}
	
	double aTerm = 1/std::pow(std::sqrt(2*CV_PI*std::pow(sigma, 2)), n);

	return aTerm*std::exp(bTerm);
}

Mat ParticleFilter::resampler(const Mat &inProbs)
{
    // resample function - for resampling the particles based on their weights
	Mat retIndex = Mat::zeros(inProbs.rows, inProbs.cols, CV_32FC1);
	
	int idx = theRNG().uniform(0, inProbs.cols-1);
	double mW;
	Mat maxProb;
	double beta = 0.0;
	// reducing to get the max for all the input feature samples
    reduce(inProbs, maxProb, 1 /*means reduced to single column*/ , CV_REDUCE_MAX);
	mW = maxProb.at<float>(0, 0);
	
	for(int i = 0; i < inProbs.cols; i++)
	{
		beta = beta + theRNG().uniform(0.0, mW);
		while(beta > inProbs.at<float>(0, idx))
		{
			beta = beta - inProbs.at<float>(0, idx);
			idx = (idx + 1)%inProbs.cols;
		}
		retIndex.at<float>(0, i) = idx; // not matlab so idx+1 is not required
		//retIndex.at<float>(0, i) = idx + 1; //  for matlab mex
	}
    return retIndex;
}