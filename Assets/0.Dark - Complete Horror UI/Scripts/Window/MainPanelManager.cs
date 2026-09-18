using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Michsky.UI.Dark
{
    public class MainPanelManager : MonoBehaviour
    {
        // Sound Options (Audio Source 2개 사용)
        [Header("Audio Settings")]
        public AudioSource openAudioSource;   // 열릴 때 쓸 오디오 소스
        public AudioSource closeAudioSource;  // 닫힐 때 쓸 오디오 소스

        // List
        public List<PanelItem> panels = new List<PanelItem>();

        // Settings
        public bool settingsHelper;
        public bool editMode;
        public bool instantInOnEnable;
        public int currentPanelIndex = 0;
        private int currentButtonIndex = 0;
        private int newPanelIndex;
        [Range(0.75f, 4)] public float disablePanelAfter = 1;
        [Range(0, 1)] public float animationSmoothness = 0.25f;
        [Range(0.75f, 4)] public float animationSpeed = 1;

        // Hidden vars
        private GameObject currentPanel;
        private GameObject nextPanel;
        private GameObject currentButton;
        private GameObject nextButton;

        private Animator currentPanelAnimator;
        private Animator nextPanelAnimator;
        private Animator currentButtonAnimator;
        private Animator nextButtonAnimator;

        // Animator state vars
        public string panelFadeIn = "Panel In";
        public string panelFadeOut = "Panel Out";
        public string panelFadeOutHelper = "Panel Out Helper";
        public string panelInstantIn = "Instant In";
        public string buttonFadeIn = "Hover to Pressed";
        public string buttonFadeOut = "Pressed to Normal";
        public string buttonFadeNormal = "Pressed to Normal";

        bool firstTime = true;
        [HideInInspector] public bool gamepadEnabled = false;

        [System.Serializable]
        public class PanelItem
        {
            public string panelName = "My Panel";
            public GameObject panelObject;
            public GameObject panelButton;
            public GameObject defaultSelected;
        }

        void Start()
        {
            // 오디오 소스가 안 지정되어 있다면 오브젝트에 붙은 AudioSource 2개를 자동으로 찾아 할당합니다.
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length > 0 && openAudioSource == null)
            {
                openAudioSource = sources[0];
            }
            if (sources.Length > 1 && closeAudioSource == null)
            {
                closeAudioSource = sources[1];
            }
        }

        // 사운드 재생 헬퍼 함수 (isOpen: true = 열기 오디오 소스 재생, false = 닫기 오디오 소스 재생)
        private void PlayPanelSound(bool isOpen)
        {
            AudioSource targetSource = isOpen ? openAudioSource : closeAudioSource;

            // closeAudioSource가 없으면 openAudioSource로 대체
            if (targetSource == null)
            {
                targetSource = openAudioSource;
            }

            if (targetSource != null)
            {
                targetSource.Play();
            }
        }

        void OnEnable()
        {
            if (panels.Count == 0 || currentPanelIndex >= panels.Count) return;

            if (panels[currentPanelIndex].panelButton != null)
            {
                currentButton = panels[currentPanelIndex].panelButton;
                currentButtonAnimator = currentButton.GetComponent<Animator>();
                if (currentButtonAnimator != null)
                    currentButtonAnimator.Play(buttonFadeIn);
            }

            currentPanel = panels[currentPanelIndex].panelObject;
            if (currentPanel != null)
            {
                currentPanel.SetActive(true);
                currentPanelAnimator = currentPanel.GetComponent<Animator>();

                if (currentPanelAnimator != null && currentPanelAnimator.gameObject.activeInHierarchy)
                {
                    if (instantInOnEnable)
                        currentPanelAnimator.Play(panelInstantIn);
                    else
                        currentPanelAnimator.Play(panelFadeIn);
                }
            }

            firstTime = false;

            for (int i = 0; i < panels.Count; i++)
            {
                if (i != currentPanelIndex && panels[i].panelObject != null)
                    panels[i].panelObject.SetActive(false);
            }
        }

        public void EnableFirstPanel()
        {
            try
            {
                panels[currentPanelIndex].panelObject.GetComponent<Animator>().Play("Instant In");
                panels[currentPanelIndex].panelButton.GetComponent<Animator>().Play("Instant In");
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(panels[currentPanelIndex].panelObject.GetComponent<RectTransform>());
            }
            catch { }
        }

        public void OpenFirstTab()
        {
            if (currentPanelIndex != 0)
                OpenPanel(panels[0].panelName);
            else if (currentPanelIndex == 0 && settingsHelper == true && firstTime == false)
            {
                OpenPanel(panels[1].panelName);
                OpenPanel(panels[0].panelName);

                if (panels[0].defaultSelected != null && gamepadEnabled == true)
                    EventSystem.current.SetSelectedGameObject(panels[0].defaultSelected);
            }
        }

        public void OpenPanel(string newPanel)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].panelName == newPanel)
                    newPanelIndex = i;
            }

            if (newPanelIndex != currentPanelIndex)
            {
                // 새 패널 번호가 더 크면 열림 소스(true), 작아지면 닫힘 소스(false) 재생
                bool isOpen = newPanelIndex > currentPanelIndex;
                PlayPanelSound(isOpen);

                StopCoroutine("DisablePreviousPanel");

                currentPanel = panels[currentPanelIndex].panelObject;
                currentPanelIndex = newPanelIndex;
                nextPanel = panels[currentPanelIndex].panelObject;
                nextPanel.SetActive(true);

                currentPanelAnimator = currentPanel.GetComponent<Animator>();
                nextPanelAnimator = nextPanel.GetComponent<Animator>();

                if (currentPanelAnimator != null)
                {
                    currentPanelAnimator.SetFloat("Anim Speed", animationSpeed);
                    currentPanelAnimator.CrossFade(panelFadeOut, animationSmoothness);
                }

                if (nextPanelAnimator != null)
                {
                    nextPanelAnimator.SetFloat("Anim Speed", animationSpeed);
                    nextPanelAnimator.CrossFade(panelFadeIn, animationSmoothness);
                }

                StartCoroutine("DisablePreviousPanel");

                if (panels[currentButtonIndex].panelButton != null)
                    currentButton = panels[currentButtonIndex].panelButton;

                currentButtonIndex = newPanelIndex;

                if (panels[currentButtonIndex].panelButton != null)
                {
                    nextButton = panels[currentButtonIndex].panelButton;
                    currentButtonAnimator = currentButton.GetComponent<Animator>();
                    nextButtonAnimator = nextButton.GetComponent<Animator>();
                    if (currentButtonAnimator != null) currentButtonAnimator.Play(buttonFadeOut);
                    if (nextButtonAnimator != null) nextButtonAnimator.Play(buttonFadeIn);
                }

                if (panels[currentPanelIndex].defaultSelected != null && gamepadEnabled == true)
                    EventSystem.current.SetSelectedGameObject(panels[currentPanelIndex].defaultSelected);
            }
        }

        public void NextPage()
        {
            if (currentPanelIndex <= panels.Count - 2)
            {
                PlayPanelSound(true); // 열기 오디오 소스 재생

                StopCoroutine("DisablePreviousPanel");

                currentPanel = panels[currentPanelIndex].panelObject;

                if (panels[currentButtonIndex].panelButton != null)
                    currentButton = panels[currentButtonIndex].panelButton;

                if (panels[currentButtonIndex + 1].panelButton != null)
                    nextButton = panels[currentButtonIndex + 1].panelButton;

                currentPanel.gameObject.SetActive(true);
                currentPanelAnimator = currentPanel.GetComponent<Animator>();

                if (currentButton != null)
                {
                    currentButtonAnimator = currentButton.GetComponent<Animator>();
                    if (currentButtonAnimator != null) currentButtonAnimator.Play(buttonFadeNormal);
                }

                if (currentPanelAnimator != null)
                {
                    currentPanelAnimator.SetFloat("Anim Speed", animationSpeed);
                    currentPanelAnimator.CrossFade(panelFadeOut, animationSmoothness);
                }

                currentPanelIndex += 1;
                currentButtonIndex += 1;
                nextPanel = panels[currentPanelIndex].panelObject;
                nextPanel.gameObject.SetActive(true);

                nextPanelAnimator = nextPanel.GetComponent<Animator>();
                if (nextPanelAnimator != null)
                {
                    nextPanelAnimator.SetFloat("Anim Speed", animationSpeed);
                    nextPanelAnimator.CrossFade(panelFadeIn, animationSmoothness);
                }

                if (nextButton != null)
                {
                    nextButtonAnimator = nextButton.GetComponent<Animator>();
                    if (nextButtonAnimator != null) nextButtonAnimator.Play(buttonFadeIn);
                }

                if (panels[currentPanelIndex].defaultSelected != null && gamepadEnabled == true)
                    EventSystem.current.SetSelectedGameObject(panels[currentPanelIndex].defaultSelected);
            }
        }

        public void PrevPage()
        {
            if (currentPanelIndex >= 1)
            {
                PlayPanelSound(false); // 닫기 오디오 소스 재생

                StopCoroutine("DisablePreviousPanel");

                currentPanel = panels[currentPanelIndex].panelObject;

                if (panels[currentButtonIndex].panelButton != null)
                    currentButton = panels[currentButtonIndex].panelButton;

                if (panels[currentButtonIndex - 1].panelButton != null)
                    nextButton = panels[currentButtonIndex - 1].panelButton;

                currentPanel.gameObject.SetActive(true);
                currentPanelAnimator = currentPanel.GetComponent<Animator>();

                if (currentButton != null)
                {
                    currentButtonAnimator = currentButton.GetComponent<Animator>();
                    if (currentButtonAnimator != null) currentButtonAnimator.Play(buttonFadeNormal);
                }

                if (currentPanelAnimator != null)
                {
                    currentPanelAnimator.SetFloat("Anim Speed", animationSpeed);
                    currentPanelAnimator.CrossFade(panelFadeOut, animationSmoothness);
                }

                currentPanelIndex -= 1;
                currentButtonIndex -= 1;
                nextPanel = panels[currentPanelIndex].panelObject;
                nextPanel.gameObject.SetActive(true);

                nextPanelAnimator = nextPanel.GetComponent<Animator>();
                if (nextPanelAnimator != null)
                {
                    nextPanelAnimator.SetFloat("Anim Speed", animationSpeed);
                    nextPanelAnimator.CrossFade(panelFadeIn, animationSmoothness);
                }

                if (nextButton != null)
                {
                    nextButtonAnimator = nextButton.GetComponent<Animator>();
                    if (nextButtonAnimator != null) nextButtonAnimator.Play(buttonFadeIn);
                }

                if (panels[currentPanelIndex].defaultSelected != null && gamepadEnabled == true)
                    EventSystem.current.SetSelectedGameObject(panels[currentPanelIndex].defaultSelected);
            }
        }

        public void AddNewItem()
        {
            PanelItem newPanel = new PanelItem();
            panels.Add(newPanel);
        }

        IEnumerator DisablePreviousPanel()
        {
            yield return new WaitForSeconds(disablePanelAfter);
            if (currentPanel != null)
                currentPanel.SetActive(false);
        }
    }
}