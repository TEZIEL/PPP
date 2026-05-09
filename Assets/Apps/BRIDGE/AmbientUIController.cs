using UnityEngine;

public class AmbientUIController : MonoBehaviour
{
    [SerializeField] private GameObject dropdown;

    public void ToggleDropdown()
    {
        dropdown.SetActive(!dropdown.activeSelf);
    }

    public void SelectWave()
    {
        AmbientManager.Instance.Play(AmbientType.Wave);
        dropdown.SetActive(false);
    }

    public void SelectFirework()
    {
        AmbientManager.Instance.Play(AmbientType.Firework);
        dropdown.SetActive(false);
    }

    public void SelectLightrain()
    {
        AmbientManager.Instance.Play(AmbientType.Lightrain);
        dropdown.SetActive(false);
    }

    public void SelectHeavyrain()
    {
        AmbientManager.Instance.Play(AmbientType.Heavyrain);
        dropdown.SetActive(false);
    }

    public void SelectMorning()
    {
        AmbientManager.Instance.Play(AmbientType.Morning);
        dropdown.SetActive(false);
    }

    public void SelectNight()
    {
        AmbientManager.Instance.Play(AmbientType.Night);
        dropdown.SetActive(false);
    }

    public void SelectCampfire()
    {
        AmbientManager.Instance.Play(AmbientType.Campfire);
        dropdown.SetActive(false);
    }

    public void SelectCafe()
    {
        AmbientManager.Instance.Play(AmbientType.Cafe);
        dropdown.SetActive(false);
    }

    public void SelectSubway()
    {
        AmbientManager.Instance.Play(AmbientType.Subway);
        dropdown.SetActive(false);
    }

    public void SelectGrocery()
    {
        AmbientManager.Instance.Play(AmbientType.Grocery);
        dropdown.SetActive(false);
    }

    public void StopAmbient()
    {
        AmbientManager.Instance.Stop();
        dropdown.SetActive(false);
    }
}