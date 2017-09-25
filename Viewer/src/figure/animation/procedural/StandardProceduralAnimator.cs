﻿public class StandardProceduralAnimator : IProceduralAnimator {
	private readonly IProceduralAnimator[] animators;
	
	public StandardProceduralAnimator(FigureModel model) {
		var channelSystem = model.ChannelSystem;
		var boneSystem = model.BoneSystem;
		var behaviorModel = model.Behavior;

		animators = new IProceduralAnimator[] {
			//new HeadLookAtAnimator(channelSystem, boneSystem),
			new EyeLookAtAnimator(channelSystem, boneSystem, behaviorModel),
			//new BreastPhysicsAnimator(channelSystem, boneSystem),
			new BlinkAnimator(channelSystem),
			//new ExpressionAnimator(channelSystem),
			//new SpeechAnimator(channelSystem, boneSystem)
		};
	}
	
	public void Update(FrameUpdateParameters updateParameters, ChannelInputs inputs) {
		foreach (var animator in animators) {
			animator.Update(updateParameters, inputs);
		}
	}
}
