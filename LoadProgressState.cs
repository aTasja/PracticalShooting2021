using CodeBase.Infrastructure.Data;
using CodeBase.Infrastructure.Data.DataTypes;
using CodeBase.Infrastructure.Factory;
using CodeBase.Server;
using CodeBase.Services.PersistentProgress;
using CodeBase.Services.SaveLoad;
using CodeBase.Services.StaticData;
using CodeBase.StaticData.Stages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


namespace CodeBase.Infrastructure.State
{
    public class LoadProgressState : IState
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly IPersistentProgressService _progressService;
        private readonly ISaveLoadService _saveLoadProgress;
        private readonly IGameFactory _gameFactory;
        private readonly IStaticDataService _staticData;

        public LoadProgressState(GameStateMachine gameStateMachine,
          IGameFactory factory, IPersistentProgressService progressService,
          ISaveLoadService saveLoadProgress, IStaticDataService staticDataService)
        {
            _gameStateMachine = gameStateMachine;
            _gameFactory = factory;
            _progressService = progressService;
            _saveLoadProgress = saveLoadProgress;
            _staticData = staticDataService;
        }

        public void Enter()
        {
            //Debug.Log("LoadProgressState");
            _gameFactory.Cleanup();
            LoadProgressOrInitNew();

            _gameStateMachine.Enter<MenuMainState>();
        }

        public void Exit()
        {
        }

        private void LoadProgressOrInitNew()
        {
            //Debug.Log("LoadOrInit");
            var loadedProgress = _saveLoadProgress.LoadProgress();

            if (loadedProgress != null) {
                _progressService.Progress = loadedProgress;
                CheckForNewCompetitions();
            } else {
                _progressService.Progress = NewProgress();
            }


            foreach (var stage in _progressService.Progress.WorldData.stagesResultsList) {
                UnityEngine.Debug.Log("comp = " + stage.competitionType + " div = " + stage.divisionType + " stage = " + stage.stageType);
            }
                
                
            
            

        }

        private PlayerProgress CheckForNewCompetitions()
        {
            List<CompetitionType> currentCompetitions = new List<CompetitionType>();
            foreach (var result in _progressService.Progress.WorldData.stagesResultsList){
                if (!currentCompetitions.Contains(result.competitionType)){
                    currentCompetitions.Add(result.competitionType);
                }
            }

            var newCompetitions = new List<string>();

            foreach(var competition in Enum.GetNames(typeof(CompetitionType))) {
                if(!currentCompetitions.Contains((CompetitionType)Enum.Parse(typeof(CompetitionType), competition))) {
                    newCompetitions.Add(competition);
                }
            }

            var newAchievements = new List<AchievementConfig>();

            foreach (var competition in newCompetitions) {
                CompetitionType compType = (CompetitionType)Enum.Parse(typeof(CompetitionType), competition);
                foreach (AchievementType achievement in _staticData.AchievementsForCompetitions(compType)) {
                    newAchievements.Add(_staticData.ForCompetitionAndAchievement(compType, achievement));
                }
            }

            _progressService.Progress.WorldData.stagesResultsList.AddRange(InitEmptyStageResults(newCompetitions.ToArray()));
            _progressService.Progress.WorldData.achievementsResultsList.AddRange(InitEmptyAchievementResultsList(newAchievements.ToArray()));

            return _progressService.Progress;
        }

        private PlayerProgress NewProgress()
        {
            //Debug.Log("NEW PROGRESS");
            var progress = new PlayerProgress(levelSceneName: "Level");

            progress.currentCompetitionType = CompetitionType.BPEO2021;
            progress.currentDivisionType = DivisionType.production;
            progress.currentStageType = StageType.stage1;

            progress.WorldData.stagesResultsList = InitEmptyStageResults(Enum.GetNames(typeof(CompetitionType)));
            progress.WorldData.achievementsResultsList = InitEmptyAchievementResultsList(_staticData.AllAchievementConfigs());
            
            return progress;
        }

        private List<StageResults> InitEmptyStageResults(string[] competitions)
        {
            var stagesResultsList = new List<StageResults>();
            
            string[] divisions = Enum.GetNames(typeof(DivisionType));
            


            foreach (var competition in competitions) {
                foreach (var division in divisions) {

                    var compEnum = (CompetitionType)Enum.Parse(typeof(CompetitionType), competition);
                    var divEnum = (DivisionType)Enum.Parse(typeof(DivisionType), division);
                    StageResults previousResults = null;

                    foreach (StageType stage in _staticData.StagesForCompetition(compEnum)) {

                        if (stage.Equals(StageType.None)) continue;                  

                        StageResults newResults = new StageResults(compEnum, divEnum, stage);

                        if (previousResults == null) previousResults = newResults;
                        else {
                            previousResults.nextStageType = newResults.stageType;
                            previousResults = newResults;
                        }

                        stagesResultsList.Add(newResults);
                    }
                }
            }
            return stagesResultsList;
        }



        private List<AchievementResults> InitEmptyAchievementResultsList(AchievementConfig[] achievements)
        {
            List<AchievementResults> achievementsResultsList = new List<AchievementResults>();
            
            string[] divisions = Enum.GetNames(typeof(DivisionType));

            foreach (AchievementConfig achievement in achievements) {
                foreach (string division in divisions) {
                    var data = _staticData.ForCompetitionAndAchievement(achievement.competitionType, achievement.achievementType);
                    var divType = (DivisionType)Enum.Parse(typeof(DivisionType), division);
                    var achRes = new AchievementResults(data.competitionType, divType, data.achievementType, data.steps);
                    achievementsResultsList.Add(achRes);
                }
            }

            return achievementsResultsList;
        }
    }

    
}