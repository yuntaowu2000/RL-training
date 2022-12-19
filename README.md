# RL-training 
## Intro
This is a course project for ECE1657 Game Theory at University of Toronto. Unity ml-agent is used to simulate a pursuit-evasion game - "The Lady in the Lake" problem, which can be formulated as a 2-player deterministic zero-sum differential game.  

## Usage
Scene - Pursuit is for the original problem as stated in 8.5 in T. Basar's Dynamic noncooperative game theory.  
Scene - Pursuit2 is for the inverse problem.  
SampleScene is some other playground not used.  

To train the models:  
- modify `pursuit_trainer_config.yaml` or create a new config following [this tutorial](https://github.com/Unity-Technologies/ml-agents/blob/release_19_docs/docs/Training-Configuration-File.md).
- In commandline, ```mlagents-learn <trainer-config-file> --env=<env_name> --run-id=batch-id```
- Hit play in Unity.
- Use ```tensorboard --logdir results --port 6006``` to monitor the training. 

Use pre-trained models for inference:  
- Copy the `.onnx` files from results into Assets.  
- Link the model to agents in `Behavior Parameters`.  

## Pre-trained models
- batch5: original problem with evader speed = 0.1*pursuer speed
- batch6: original problem with evader speed = 0.8*pursuer speed (not used)
- batch7: original problem with evader speed = 0.4*pursuer speed
- inversebatch3: inverse problem with pursuer speed = 0.8*evader speed
- inversebatch5: inverse problem with pursuer speed = 0.5*evader speed

## Environment
Unity 2022.1.6f1 with [ml-agents release 19](https://github.com/Unity-Technologies/ml-agents/tree/release_19_docs).  