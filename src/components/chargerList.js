import './chargerList.css';
import chargerImage from './images/Zaptec.jpg';

const chargersLists = [
    {
      id: 1,
      batteryLevel: 37,
      image: chargerImage
    },
    {
      id: 2,
      batteryLevel: 32,
      image: chargerImage
    },
    {
      id: 3,
      batteryLevel: 43,
      image: chargerImage
    },
    {
      id: 4,
      batteryLevel: 56,
      image: chargerImage
    },
    {
        id: 5,
        batteryLevel: 65,
        image: chargerImage
      },
      {
        id: 6,
        batteryLevel: 73,
        image: chargerImage
      },
      {
        id: 7,
        batteryLevel: 85,
        image: chargerImage
      },
      {
        id: 8,
        batteryLevel: 97,
        image: chargerImage
      }
  ];

export default function ChargerList() {
    return (
        <div className='contents'>
            {chargersLists.map((charger) => (
        <div key={charger.id} className="chargerContainer">
          <img src={charger.image} className="chargerImage" alt="chargerImage" />
          <div className='batteryLevel'><div></div></div>
          <div className='batterypercentage'>{charger.batteryLevel}%</div>
          <button>Connect</button>
        </div>
      ))}
           
        </div>
    )
}
