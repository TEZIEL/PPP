using UnityEngine;

public class AmbientUIController : MonoBehaviour
{
    [SerializeField] private GameObject dropdown;

    public void ToggleDropdown()
    {
        dropdown.SetActive(!dropdown.activeSelf);
    }

    public void SelectOcean()
    {
        AmbientManager.Instance.Play(AmbientType.Ocean);
        dropdown.SetActive(false);
    }

    public void SelectStorm()
    {
        AmbientManager.Instance.Play(AmbientType.Storm);
        dropdown.SetActive(false);
    }

    public void SelectLightRain()
    {
        AmbientManager.Instance.Play(AmbientType.LightRain);
        dropdown.SetActive(false);
    }

    public void SelectMorningBirds()
    {
        AmbientManager.Instance.Play(AmbientType.MorningBirds);
        dropdown.SetActive(false);
    }

    public void SelectNightCrickets()
    {
        AmbientManager.Instance.Play(AmbientType.NightCrickets);
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

    public void SelectCityStreet()
    {
        AmbientManager.Instance.Play(AmbientType.CityStreet);
        dropdown.SetActive(false);
    }

    public void SelectDreamcore()
    {
        AmbientManager.Instance.Play(AmbientType.Dreamcore);
        dropdown.SetActive(false);
    }

    public void SelectCampfire()
    {
        AmbientManager.Instance.Play(AmbientType.Campfire);
        dropdown.SetActive(false);
    }

    public void StopAmbient()
    {
        AmbientManager.Instance.Stop();
        dropdown.SetActive(false);
    }
}